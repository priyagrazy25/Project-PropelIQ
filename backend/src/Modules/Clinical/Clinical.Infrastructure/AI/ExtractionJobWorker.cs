using Clinical.Application.AI;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Background worker that processes extraction jobs from the queue.
/// Orchestrates full pipeline: OCR → NER → Embedding → Complete.
/// Uses SemaphoreSlim(2) via ExtractionJobQueue for max 2 concurrent pipelines (AIR-O04).
/// </summary>
public sealed class ExtractionJobWorker : BackgroundService
{
    private readonly IExtractionJobQueue _jobQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExtractionJobWorker> _logger;

    private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(10);

    public ExtractionJobWorker(
        IExtractionJobQueue jobQueue,
        IServiceProvider serviceProvider,
        ILogger<ExtractionJobWorker> logger)
    {
        _jobQueue = jobQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Extraction Job Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Dequeue blocks until a job is available (and acquires concurrency slot)
                var job = await _jobQueue.DequeueAsync(stoppingToken);

                if (job is null)
                {
                    continue;
                }

                // Process job with timeout
                _ = ProcessJobWithTimeoutAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in extraction job worker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Extraction Job Worker stopped");
    }

    private async Task ProcessJobWithTimeoutAsync(ExtractionJob job, CancellationToken stoppingToken)
    {
        using var timeoutCts = new CancellationTokenSource(JobTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

        try
        {
            _logger.LogInformation("Processing extraction job {JobId} for document {DocumentId}", job.JobId, job.DocumentId);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();

            // Load document
            var document = await dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == job.DocumentId, linkedCts.Token);

            if (document is null)
            {
                await _jobQueue.CompleteJobAsync(job.JobId, success: false, error: "Document not found");
                return;
            }

            // Process through pipeline stages
            var success = await ProcessPipelineAsync(scope.ServiceProvider, document.Id, linkedCts.Token);

            if (success)
            {
                // Mark document as complete
                document.ProcessingStatus = ProcessingStatus.Completed;
                document.ProcessedAt = DateTime.UtcNow;
                document.ProcessingError = null;
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }

            await _jobQueue.CompleteJobAsync(job.JobId, success, success ? null : "Pipeline processing failed");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning("Extraction job {JobId} timed out after {Timeout}", job.JobId, JobTimeout);
            await _jobQueue.CompleteJobAsync(job.JobId, success: false, error: $"Job timed out after {JobTimeout.TotalMinutes} minutes");

            await MarkDocumentAsFailedAsync(job.DocumentId, "Processing timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction job {JobId} failed with exception", job.JobId);
            await _jobQueue.CompleteJobAsync(job.JobId, success: false, error: ex.Message);

            await MarkDocumentAsFailedAsync(job.DocumentId, ex.Message);
        }
    }

    private async Task<bool> ProcessPipelineAsync(IServiceProvider serviceProvider, Guid documentId, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<ClinicalDbContext>();

        // Get current document status
        var document = await dbContext.ClinicalDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return false;
        }

        // Stage 1: OCR (if pending)
        if (document.ProcessingStatus == ProcessingStatus.Pending)
        {
            var ocrPipeline = serviceProvider.GetRequiredService<IOcrExtractionPipeline>();
            var ocrResult = await ocrPipeline.ProcessDocumentAsync(documentId, cancellationToken);

            if (!ocrResult.IsSuccess)
            {
                _logger.LogWarning("OCR pipeline failed for document {DocumentId}: {Error}", documentId, ocrResult.Error);
                return false;
            }

            _logger.LogInformation("OCR complete for document {DocumentId}: {Chunks} chunks created", documentId, ocrResult.Value!.ChunksCreated);

            // Reload document status
            document = await dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        }

        // Stage 2: NER (if ready)
        if (document?.ProcessingStatus == ProcessingStatus.NerInProgress)
        {
            var nerPipeline = serviceProvider.GetRequiredService<INerExtractionPipeline>();
            var nerResult = await nerPipeline.ProcessDocumentAsync(documentId, cancellationToken);

            if (!nerResult.IsSuccess)
            {
                _logger.LogWarning("NER pipeline failed for document {DocumentId}: {Error}", documentId, nerResult.Error);
                return false;
            }

            _logger.LogInformation("NER complete for document {DocumentId}: {Entities} entities extracted", documentId, nerResult.Value!.EntitiesExtracted);

            // Reload document status
            document = await dbContext.ClinicalDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        }

        // Stage 3: Embedding (if ready)
        if (document?.ProcessingStatus == ProcessingStatus.CodingInProgress)
        {
            var embeddingService = serviceProvider.GetRequiredService<IEmbeddingService>();
            var embeddingResult = await embeddingService.GenerateDocumentEmbeddingsAsync(documentId, cancellationToken);

            if (!embeddingResult.IsSuccess)
            {
                _logger.LogWarning("Embedding generation failed for document {DocumentId}: {Error}", documentId, embeddingResult.Error);
                // Continue without embeddings - they're optional for basic functionality
            }
            else
            {
                _logger.LogInformation("Embeddings complete for document {DocumentId}: {Count} embeddings generated", documentId, embeddingResult.Value!.EmbeddingsGenerated);
            }
        }

        return true;
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
