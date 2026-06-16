using Clinical.Application.AI;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Background worker that polls for pending documents and processes them through OCR pipeline.
/// Implements 5-minute timeout per document (NFR-003).
/// </summary>
public sealed class OcrProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OcrProcessingWorker> _logger;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);

    public OcrProcessingWorker(
        IServiceProvider serviceProvider,
        ILogger<OcrProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OCR Processing Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDocumentsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in OCR processing worker loop");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OCR Processing Worker stopped");
    }

    private async Task ProcessPendingDocumentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();

        // Find documents with Pending status (ready for OCR processing)
        var pendingDocuments = await dbContext.ClinicalDocuments
            .Where(d => d.ProcessingStatus == ProcessingStatus.Pending)
            .OrderBy(d => d.CreatedAt)
            .Take(5) // Process up to 5 at a time
            .Select(d => d.Id)
            .ToListAsync(stoppingToken);

        if (pendingDocuments.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} pending documents for OCR processing", pendingDocuments.Count);

        foreach (var documentId in pendingDocuments)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessDocumentWithTimeoutAsync(documentId, stoppingToken);
        }
    }

    private async Task ProcessDocumentWithTimeoutAsync(Guid documentId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IOcrExtractionPipeline>();

            // Create combined cancellation token with 5-minute timeout (NFR-003)
            using var timeoutCts = new CancellationTokenSource(ProcessingTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

            _logger.LogInformation("Processing document {DocumentId} through OCR pipeline", documentId);

            var result = await pipeline.ProcessDocumentAsync(documentId, linkedCts.Token);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "OCR processing complete for document {DocumentId}: {ChunkCount} chunks in {TimeMs}ms",
                    documentId, result.Value!.ChunksCreated, result.Value.ProcessingTimeMs);
            }
            else
            {
                _logger.LogWarning(
                    "OCR processing failed for document {DocumentId}: {Error}",
                    documentId, result.Error);
            }
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Timeout occurred
            _logger.LogWarning(
                "OCR processing timed out for document {DocumentId} (exceeded {Timeout} timeout)",
                documentId, ProcessingTimeout);

            await MarkDocumentAsFailedAsync(documentId, "Processing timed out (exceeded 5 minutes)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing document {DocumentId}", documentId);

            await MarkDocumentAsFailedAsync(documentId, $"Unexpected error: {ex.Message}");
        }
    }

    private async Task MarkDocumentAsFailedAsync(Guid documentId, string error)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();

            var document = await dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document is not null)
            {
                document.ProcessingStatus = ProcessingStatus.Failed;
                document.ProcessingError = error;
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark document {DocumentId} as failed", documentId);
        }
    }
}
