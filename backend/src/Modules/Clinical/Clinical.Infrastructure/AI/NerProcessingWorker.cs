using Clinical.Application.AI;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Background worker that polls for documents ready for NER processing.
/// Picks up documents with status NerInProgress from OCR pipeline.
/// </summary>
public sealed class NerProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NerProcessingWorker> _logger;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(3);

    public NerProcessingWorker(
        IServiceProvider serviceProvider,
        ILogger<NerProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NER Processing Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDocumentsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in NER processing worker loop");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("NER Processing Worker stopped");
    }

    private async Task ProcessPendingDocumentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();

        // Find documents with NerInProgress status (ready for NER from OCR pipeline)
        var pendingDocuments = await dbContext.ClinicalDocuments
            .Where(d => d.ProcessingStatus == ProcessingStatus.NerInProgress)
            .OrderBy(d => d.ProcessedAt ?? d.CreatedAt)
            .Take(3) // Process up to 3 at a time (NER is more resource intensive)
            .Select(d => d.Id)
            .ToListAsync(stoppingToken);

        if (pendingDocuments.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} documents ready for NER processing", pendingDocuments.Count);

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
            var pipeline = scope.ServiceProvider.GetRequiredService<INerExtractionPipeline>();

            // Create combined cancellation token with timeout
            using var timeoutCts = new CancellationTokenSource(ProcessingTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

            _logger.LogInformation("Processing document {DocumentId} through NER pipeline", documentId);

            var result = await pipeline.ProcessDocumentAsync(documentId, linkedCts.Token);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "NER processing complete for document {DocumentId}: {EntityCount} entities, {LowConfCount} low confidence in {TimeMs}ms",
                    documentId, result.Value!.EntitiesExtracted, result.Value.LowConfidenceCount, result.Value.ProcessingTimeMs);
            }
            else
            {
                _logger.LogWarning(
                    "NER processing failed for document {DocumentId}: {Error}",
                    documentId, result.Error);
            }
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Timeout occurred
            _logger.LogWarning(
                "NER processing timed out for document {DocumentId} (exceeded {Timeout} timeout)",
                documentId, ProcessingTimeout);

            await MarkDocumentAsFailedAsync(documentId, "NER processing timed out");
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
