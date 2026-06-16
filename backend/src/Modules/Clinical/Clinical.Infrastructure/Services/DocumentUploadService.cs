using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Document upload service handling encrypted storage and entity creation (AC-2, AC-5, DR-004).
/// </summary>
public sealed class DocumentUploadService : IDocumentUploadService
{
    private readonly IDocumentStorageService _storageService;
    private readonly ClinicalDbContext _dbContext;
    private readonly IPatientView360Service _patientView360Service;
    private readonly ILogger<DocumentUploadService> _logger;

    public DocumentUploadService(
        IDocumentStorageService storageService,
        ClinicalDbContext dbContext,
        IPatientView360Service patientView360Service,
        ILogger<DocumentUploadService> logger)
    {
        _storageService = storageService;
        _dbContext = dbContext;
        _patientView360Service = patientView360Service;
        _logger = logger;
    }

    public async Task<Result<DocumentUploadResponse>> UploadDocumentAsync(
        Guid patientId,
        Guid? userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        // Store document to encrypted storage (DR-004)
        var storeResult = await _storageService.StoreDocumentAsync(
            patientId,
            fileName,
            contentType,
            stream,
            cancellationToken);

        if (!storeResult.IsSuccess)
        {
            return Result<DocumentUploadResponse>.Failure(storeResult.Error!);
        }

        // Create ClinicalDocument entity with "Pending" status (AC-2, AC-5)
        // Note: Id and CreatedAt are auto-initialized by BaseEntity
        var document = new ClinicalDocument
        {
            PatientId = patientId,
            FileName = SanitizeFileName(fileName),
            EncryptedFilePath = storeResult.Value!,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            ProcessingStatus = ProcessingStatus.Pending,
            UploadedByUserId = userId
        };

        _dbContext.ClinicalDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate 360 view cache so document count updates immediately
        await _patientView360Service.InvalidateCacheAsync(patientId, cancellationToken);

        _logger.LogInformation(
            "Document {DocumentId} uploaded for patient {PatientId}, queued for processing",
            document.Id, patientId);

        // TODO: Enqueue extraction job to processing queue (Hangfire/background service)
        // await _jobQueue.EnqueueAsync<DocumentExtractionJob>(document.Id);

        return Result<DocumentUploadResponse>.Success(new DocumentUploadResponse(
            document.Id,
            document.FileName,
            "Queued"));
    }

    public async Task<Result<DocumentUploadResponse>> GetDocumentStatusAsync(
        Guid documentId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.ClinicalDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null || document.PatientId != patientId)
        {
            return Result<DocumentUploadResponse>.Failure("Document not found.");
        }

        var statusString = document.ProcessingStatus switch
        {
            ProcessingStatus.Pending => "Queued",
            ProcessingStatus.Uploading => "Queued",
            ProcessingStatus.OcrInProgress => "Processing",
            ProcessingStatus.NerInProgress => "Processing",
            ProcessingStatus.CodingInProgress => "Processing",
            ProcessingStatus.Completed => "Complete",
            ProcessingStatus.Failed => "Failed",
            _ => "Unknown"
        };

        return Result<DocumentUploadResponse>.Success(new DocumentUploadResponse(
            document.Id,
            document.FileName,
            statusString));
    }

    public async Task<Result<IReadOnlyList<DocumentPipelineStatusResponse>>> GetAllDocumentsStatusAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var documents = await _dbContext.ClinicalDocuments
            .AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var results = documents.Select(d => MapToPipelineStatus(d)).ToList();
        return Result<IReadOnlyList<DocumentPipelineStatusResponse>>.Success(results);
    }

    /// <summary>
    /// Maps ClinicalDocument to pipeline status response for SCR-015.
    /// </summary>
    private static DocumentPipelineStatusResponse MapToPipelineStatus(ClinicalDocument doc)
    {
        var (status, currentStep, completedSteps, progress) = doc.ProcessingStatus switch
        {
            ProcessingStatus.Pending => ("Queued", "OCR", Array.Empty<string>(), 5),
            ProcessingStatus.Uploading => ("Queued", "OCR", Array.Empty<string>(), 10),
            ProcessingStatus.OcrInProgress => ("Processing", "OCR", Array.Empty<string>(), 25),
            ProcessingStatus.NerInProgress => ("Processing", "NER", new[] { "OCR" }, 50),
            ProcessingStatus.CodingInProgress => ("Processing", "Coding", new[] { "OCR", "NER" }, 75),
            ProcessingStatus.Completed => ("Complete", "Validation", new[] { "OCR", "NER", "Coding", "Validation" }, 100),
            ProcessingStatus.Failed => ("Failed", doc.ProcessingError?.Contains("NER") == true ? "NER" :
                doc.ProcessingError?.Contains("Coding") == true ? "Coding" : "OCR",
                Array.Empty<string>(), 0),
            _ => ("Queued", "OCR", Array.Empty<string>(), 0)
        };

        return new DocumentPipelineStatusResponse(
            doc.Id,
            doc.FileName,
            status,
            currentStep,
            completedSteps,
            progress);
    }

    /// <summary>
    /// Sanitizes file name to prevent path traversal attacks.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 255 ? sanitized[..255] : sanitized;
    }
}
