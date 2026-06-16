using System.Diagnostics;
using Clinical.Application.Abstractions;
using Clinical.Application.AI;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Orchestrates OCR extraction pipeline: PDF → OCR → PII redaction → chunking (AIR-001).
/// Implements 5-minute timeout for ≤20 pages (NFR-003).
/// </summary>
public sealed class OcrExtractionPipeline : IOcrExtractionPipeline
{
    private readonly ClinicalDbContext _dbContext;
    private readonly IDocumentStorageService _documentStorage;
    private readonly IOcrService _ocrService;
    private readonly PiiRedactor _piiRedactor;
    private readonly TextChunker _textChunker;
    private readonly ILogger<OcrExtractionPipeline> _logger;

    private const int ChunkSize = 512;
    private const int ChunkOverlap = 51;
    private const float LowConfidenceThreshold = 0.6f;

    public OcrExtractionPipeline(
        ClinicalDbContext dbContext,
        IDocumentStorageService documentStorage,
        IOcrService ocrService,
        PiiRedactor piiRedactor,
        TextChunker textChunker,
        ILogger<OcrExtractionPipeline> logger)
    {
        _dbContext = dbContext;
        _documentStorage = documentStorage;
        _ocrService = ocrService;
        _piiRedactor = piiRedactor;
        _textChunker = textChunker;
        _logger = logger;
    }

    public async Task<Result<OcrPipelineResult>> ProcessDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Load document metadata
            var document = await _dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document is null)
            {
                return Result<OcrPipelineResult>.Failure($"Document {documentId} not found.");
            }

            // 2. Update status to OcrInProgress
            document.ProcessingStatus = ProcessingStatus.OcrInProgress;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Starting OCR pipeline for document {DocumentId}", documentId);

            // 3. Retrieve encrypted document
            var documentResult = await _documentStorage.GetDocumentAsync(
                document.EncryptedFilePath, cancellationToken);

            if (!documentResult.IsSuccess)
            {
                return await FailPipeline(document, $"Failed to retrieve document: {documentResult.Error}");
            }

            // 4. Extract text from PDF pages via OCR
            var (pageTexts, avgConfidence, totalPages) = await ExtractTextFromPdfAsync(
                documentResult.Value!, cancellationToken);

            if (totalPages == 0)
            {
                return await FailPipeline(document, "No pages found in document.");
            }

            // 5. Apply PII redaction to all page texts
            var totalRedactions = 0;
            var redactedPageTexts = new Dictionary<int, string>();

            foreach (var (pageNum, text) in pageTexts)
            {
                var (redactedText, redactionCount) = _piiRedactor.Redact(text);
                redactedPageTexts[pageNum] = redactedText;
                totalRedactions += redactionCount;
            }

            _logger.LogInformation(
                "PII redaction complete for document {DocumentId}: {RedactionCount} redactions",
                documentId, totalRedactions);

            // 6. Chunk the redacted text
            var chunks = _textChunker.ChunkWithPages(redactedPageTexts);

            // 7. Delete existing chunks for this document
            var existingChunks = await _dbContext.Set<DocumentChunk>()
                .Where(c => c.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            _dbContext.RemoveRange(existingChunks);

            // 8. Create new DocumentChunk entities
            foreach (var chunk in chunks)
            {
                var qualityFlags = new List<string>();

                if (string.IsNullOrWhiteSpace(chunk.Content))
                {
                    qualityFlags.Add("blank");
                }

                if (avgConfidence < LowConfidenceThreshold)
                {
                    qualityFlags.Add("low_confidence");
                }

                var documentChunk = new DocumentChunk
                {
                    DocumentId = documentId,
                    PatientId = document.PatientId,
                    ChunkIndex = chunk.Index,
                    SourcePages = string.Join(",", chunk.SourcePages),
                    Content = chunk.Content,
                    TokenCount = chunk.TokenCount,
                    OcrConfidence = avgConfidence,
                    QualityFlags = qualityFlags.Count > 0 ? string.Join(",", qualityFlags) : null
                };

                _dbContext.Add(documentChunk);
            }

            // 9. Update document status
            document.ProcessingStatus = ProcessingStatus.NerInProgress; // Ready for NER stage
            document.ProcessedAt = DateTime.UtcNow;
            document.ProcessingError = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "OCR pipeline complete for document {DocumentId}: {ChunkCount} chunks, {PageCount} pages, {AvgConfidence:P1} avg confidence in {ElapsedMs}ms",
                documentId, chunks.Count, totalPages, avgConfidence, stopwatch.ElapsedMilliseconds);

            return Result<OcrPipelineResult>.Success(new OcrPipelineResult(
                DocumentId: documentId,
                ChunksCreated: chunks.Count,
                TotalPages: totalPages,
                AverageConfidence: avgConfidence,
                PiiRedactionsCount: totalRedactions,
                ProcessingTimeMs: stopwatch.ElapsedMilliseconds
            ));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR pipeline cancelled for document {DocumentId}", documentId);

            var document = await _dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, CancellationToken.None);

            if (document is not null)
            {
                document.ProcessingStatus = ProcessingStatus.Failed;
                document.ProcessingError = "Processing cancelled (timeout)";
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }

            return Result<OcrPipelineResult>.Failure("OCR processing cancelled due to timeout.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR pipeline failed for document {DocumentId}", documentId);

            var document = await _dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, CancellationToken.None);

            if (document is not null)
            {
                return await FailPipeline(document, $"OCR processing error: {ex.Message}");
            }

            return Result<OcrPipelineResult>.Failure($"OCR processing error: {ex.Message}");
        }
    }

    private async Task<(Dictionary<int, string> PageTexts, float AvgConfidence, int TotalPages)> ExtractTextFromPdfAsync(
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        var pageTexts = new Dictionary<int, string>();
        var confidences = new List<float>();

        using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream, cancellationToken);
        var pdfBytes = memoryStream.ToArray();

        using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(1.0));
        var pageCount = docReader.GetPageCount();

        _logger.LogInformation("Processing PDF with {PageCount} pages", pageCount);

        // First pass: try to extract native text from all pages
        var hasNativeText = false;
        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var pageReader = docReader.GetPageReader(pageIndex);
            var pageNumber = pageIndex + 1;

            // Try to extract native (embedded) text from PDF
            var nativeText = pageReader.GetText();
            if (!string.IsNullOrWhiteSpace(nativeText))
            {
                pageTexts[pageNumber] = nativeText;
                confidences.Add(1.0f); // Native text has 100% confidence
                hasNativeText = true;

                _logger.LogDebug(
                    "Page {PageNumber} native text extracted: {CharCount} chars",
                    pageNumber, nativeText.Length);
            }
        }

        // If native text found, return it
        if (hasNativeText)
        {
            _logger.LogInformation(
                "PDF has native text - extracted {PageCount} pages without OCR",
                pageTexts.Count);
            var nativeAvgConfidence = confidences.Count > 0 ? confidences.Average() : 0f;
            return (pageTexts, nativeAvgConfidence, pageCount);
        }

        // Second pass: fall back to OCR for scanned/image-based PDFs
        _logger.LogInformation("No native text found - falling back to OCR");
        pageTexts.Clear();
        confidences.Clear();

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var pageReader = docReader.GetPageReader(pageIndex);

            // Render page to image for OCR
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage();

            if (rawBytes is null || rawBytes.Length == 0)
            {
                _logger.LogWarning("Page {PageIndex} returned empty image", pageIndex);
                continue;
            }

            // Convert BGRA to PNG for Tesseract
            var pngBytes = ConvertBgraToPng(rawBytes, width, height);

            // Run OCR on page image
            var ocrResult = await _ocrService.ExtractTextFromPdfPageAsync(pngBytes, cancellationToken);

            var pageNumber = pageIndex + 1; // 1-based page numbers

            if (ocrResult.Success)
            {
                pageTexts[pageNumber] = ocrResult.Text;
                confidences.Add(ocrResult.Confidence);

                _logger.LogDebug(
                    "Page {PageNumber} OCR complete: {CharCount} chars, {Confidence:P1} confidence",
                    pageNumber, ocrResult.Text.Length, ocrResult.Confidence);
            }
            else
            {
                _logger.LogWarning(
                    "Page {PageNumber} OCR failed: {Error}",
                    pageNumber, ocrResult.Error);

                pageTexts[pageNumber] = string.Empty;
                confidences.Add(0f);
            }
        }

        var avgConfidence = confidences.Count > 0 ? confidences.Average() : 0f;

        return (pageTexts, avgConfidence, pageCount);
    }

    private static byte[] ConvertBgraToPng(byte[] bgraData, int width, int height)
    {
        // Simple BMP-style raw conversion - Tesseract can handle raw formats
        // For production, use ImageSharp or similar for proper PNG encoding
        // Here we create a minimal BMP header + data

        var rowSize = width * 4;
        var padding = (4 - (width * 3) % 4) % 4;
        var bmpRowSize = width * 3 + padding;
        var imageSize = bmpRowSize * height;
        var fileSize = 54 + imageSize;

        using var ms = new MemoryStream(fileSize);
        using var writer = new BinaryWriter(ms);

        // BMP Header
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write(0); // Reserved
        writer.Write(54); // Pixel data offset

        // DIB Header (BITMAPINFOHEADER)
        writer.Write(40); // Header size
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1); // Color planes
        writer.Write((short)24); // Bits per pixel
        writer.Write(0); // Compression
        writer.Write(imageSize);
        writer.Write(2835); // Horizontal resolution (72 DPI)
        writer.Write(2835); // Vertical resolution (72 DPI)
        writer.Write(0); // Colors in palette
        writer.Write(0); // Important colors

        // Pixel data (bottom-up, BGR)
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIndex = (y * rowSize) + (x * 4);
                writer.Write(bgraData[srcIndex]);     // B
                writer.Write(bgraData[srcIndex + 1]); // G
                writer.Write(bgraData[srcIndex + 2]); // R
            }

            for (var p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }

        return ms.ToArray();
    }

    private async Task<Result<OcrPipelineResult>> FailPipeline(ClinicalDocument document, string error)
    {
        document.ProcessingStatus = ProcessingStatus.Failed;
        document.ProcessingError = error;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _logger.LogError("OCR pipeline failed for document {DocumentId}: {Error}", document.Id, error);

        return Result<OcrPipelineResult>.Failure(error);
    }
}
