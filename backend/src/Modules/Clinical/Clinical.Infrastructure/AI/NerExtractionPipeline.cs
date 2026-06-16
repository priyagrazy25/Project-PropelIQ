using System.Diagnostics;
using Clinical.Application.AI;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Orchestrates NER extraction pipeline: chunks → NER → ExtractedData storage (AIR-001).
/// Implements confidence scoring (AIR-Q04), schema validation (AIR-Q03), and low-confidence flagging (DR-010).
/// </summary>
public sealed class NerExtractionPipeline : INerExtractionPipeline
{
    private readonly ClinicalDbContext _dbContext;
    private readonly INerService _nerService;
    private readonly ILogger<NerExtractionPipeline> _logger;
    private readonly IExtractionJobQueue _jobQueue;

    private const float LowConfidenceThreshold = 0.7f;
    private const float TargetSchemaValidity = 99.0f;

    public NerExtractionPipeline(
        ClinicalDbContext dbContext,
        INerService nerService,
        ILogger<NerExtractionPipeline> logger,
        IExtractionJobQueue jobQueue)
    {
        _dbContext = dbContext;
        _nerService = nerService;
        _logger = logger;
        _jobQueue = jobQueue;
    }

    public async Task<Result<NerPipelineResult>> ProcessDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Load document and verify status
            var document = await _dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document is null)
            {
                return Result<NerPipelineResult>.Failure($"Document {documentId} not found.");
            }

            // Document should be in NerInProgress status (set by OCR pipeline)
            if (document.ProcessingStatus != ProcessingStatus.NerInProgress)
            {
                _logger.LogWarning(
                    "Document {DocumentId} not ready for NER (status: {Status})",
                    documentId, document.ProcessingStatus);

                return Result<NerPipelineResult>.Failure(
                    $"Document not ready for NER processing. Current status: {document.ProcessingStatus}");
            }

            _logger.LogInformation("Starting NER pipeline for document {DocumentId}", documentId);

            // 2. Load document chunks
            var chunks = await _dbContext.Set<DocumentChunk>()
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync(cancellationToken);

            if (chunks.Count == 0)
            {
                return await FailPipeline(document, "No text chunks found for NER processing.");
            }

            _logger.LogInformation("Processing {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);

            // 3. Check NER service availability
            if (!await _nerService.IsAvailableAsync(cancellationToken))
            {
                return await FailPipeline(document, "NER service unavailable.");
            }

            // 4. Process each chunk through NER
            var totalEntitiesExtracted = 0;
            var totalLowConfidenceCount = 0;
            var totalSchemaValidity = 0f;
            var chunkResults = new List<(DocumentChunk Chunk, ClinicalNerResult Result)>();

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Parse source pages (comma-separated)
                var sourcePages = chunk.SourcePages.Split(',')
                    .Select(p => int.TryParse(p.Trim(), out var page) ? page : (int?)null)
                    .FirstOrDefault();

                var nerResult = await _nerService.ExtractClinicalEntitiesAsync(
                    text: chunk.Content,
                    context: null,
                    sourcePage: sourcePages,
                    cancellationToken: cancellationToken);

                if (!nerResult.Success)
                {
                    _logger.LogWarning(
                        "NER extraction failed for chunk {ChunkIndex}: {Error}",
                        chunk.ChunkIndex, nerResult.Error);
                    continue;
                }

                chunkResults.Add((chunk, nerResult));
                totalEntitiesExtracted += nerResult.TotalCount;
                totalLowConfidenceCount += nerResult.LowConfidenceCount;
                totalSchemaValidity += nerResult.SchemaValidityPercent;
            }

            // Calculate average schema validity
            var avgSchemaValidity = chunkResults.Count > 0
                ? totalSchemaValidity / chunkResults.Count
                : 100f;

            // 5. Delete existing extracted data for this document
            var existingData = await _dbContext.ExtractedData
                .Where(e => e.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            _dbContext.RemoveRange(existingData);

            // 6. Store ExtractedData records
            foreach (var (chunk, nerResult) in chunkResults)
            {
                foreach (var entity in nerResult.Entities)
                {
                    var category = MapToDataCategory(entity.Category);

                    var extractedData = new ExtractedData
                    {
                        DocumentId = documentId,
                        PatientId = document.PatientId,
                        Category = category,
                        Key = entity.Key,
                        Value = FormatValue(entity.Value, entity.Unit),
                        ConfidenceScore = entity.Confidence,
                        SourcePage = entity.SourcePage,
                        SourceText = entity.Text,
                    };

                    _dbContext.Add(extractedData);
                }
            }

            // 7. Update document status
            document.ProcessingStatus = ProcessingStatus.CodingInProgress; // Ready for medical coding
            document.ProcessingError = null;

            // Log warning if schema validity below target
            if (avgSchemaValidity < TargetSchemaValidity)
            {
                _logger.LogWarning(
                    "Document {DocumentId} NER schema validity {Validity:F2}% below target {Target}%",
                    documentId, avgSchemaValidity, TargetSchemaValidity);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Enqueue job for coding/embedding step
            await _jobQueue.EnqueueAsync(documentId, priority: 0);

            stopwatch.Stop();

            _logger.LogInformation(
                "NER pipeline complete for document {DocumentId}: {EntityCount} entities, {LowConfCount} low confidence, {Validity:F2}% schema validity in {ElapsedMs}ms",
                documentId, totalEntitiesExtracted, totalLowConfidenceCount, avgSchemaValidity, stopwatch.ElapsedMilliseconds);

            return Result<NerPipelineResult>.Success(new NerPipelineResult(
                DocumentId: documentId,
                EntitiesExtracted: totalEntitiesExtracted,
                LowConfidenceCount: totalLowConfidenceCount,
                SchemaValidityPercent: avgSchemaValidity,
                ProcessingTimeMs: stopwatch.ElapsedMilliseconds
            ));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("NER pipeline cancelled for document {DocumentId}", documentId);

            var document = await _dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, CancellationToken.None);

            if (document is not null)
            {
                document.ProcessingStatus = ProcessingStatus.Failed;
                document.ProcessingError = "NER processing cancelled";
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }

            return Result<NerPipelineResult>.Failure("NER processing cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NER pipeline failed for document {DocumentId}", documentId);

            var document = await _dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, CancellationToken.None);

            if (document is not null)
            {
                return await FailPipeline(document, $"NER processing error: {ex.Message}");
            }

            return Result<NerPipelineResult>.Failure($"NER processing error: {ex.Message}");
        }
    }

    private static DataCategory MapToDataCategory(string category)
    {
        return category switch
        {
            "Diagnosis" => DataCategory.Diagnosis,
            "Medication" => DataCategory.Medication,
            "Allergy" => DataCategory.Allergy,
            "Procedure" => DataCategory.Procedure,
            "LabResult" => DataCategory.LabResult,
            "VitalSign" => DataCategory.VitalSign,
            "Symptom" => DataCategory.Symptom,
            "FamilyHistory" => DataCategory.FamilyHistory,
            _ => DataCategory.Diagnosis // Default
        };
    }

    private static string FormatValue(string value, string? unit)
    {
        if (string.IsNullOrEmpty(unit))
        {
            return value;
        }

        return $"{value} {unit}";
    }

    private async Task<Result<NerPipelineResult>> FailPipeline(ClinicalDocument document, string error)
    {
        document.ProcessingStatus = ProcessingStatus.Failed;
        document.ProcessingError = error;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _logger.LogError("NER pipeline failed for document {DocumentId}: {Error}", document.Id, error);

        return Result<NerPipelineResult>.Failure(error);
    }
}
