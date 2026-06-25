using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using StackExchange.Redis;

namespace SharedKernel.Audit;

/// <summary>
/// Background service that drains the audit retry queue every 30 seconds (FR-031).
/// Ensures eventual consistency for audit records that failed initial write.
/// </summary>
public sealed class AuditRetryProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<AuditRetryProcessor> _logger;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
    private const string AuditRetryQueueKey = "audit:retry:queue";
    private const string AiAuditRetryQueueKey = "audit:ai:retry:queue";
    private const int MaxBatchSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditRetryProcessor(
        IServiceProvider serviceProvider,
        IConnectionMultiplexer? redis,
        ILogger<AuditRetryProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_redis == null)
        {
            _logger.LogWarning("AuditRetryProcessor disabled: Redis not available");
            return;
        }

        _logger.LogInformation("AuditRetryProcessor started — processing every {Interval}s", RetryInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RetryInterval, stoppingToken);
                await ProcessRetryQueueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audit retry queue");
            }
        }

        _logger.LogInformation("AuditRetryProcessor stopped");
    }

    private async Task ProcessRetryQueueAsync(CancellationToken cancellationToken)
    {
        var db = _redis!.GetDatabase();

        // Process regular audit logs
        await ProcessQueueAsync<AuditLog>(db, AuditRetryQueueKey, cancellationToken);

        // Process AI audit logs
        await ProcessQueueAsync<AiInvocationAuditLog>(db, AiAuditRetryQueueKey, cancellationToken);
    }

    private async Task ProcessQueueAsync<T>(IDatabase db, string queueKey, CancellationToken cancellationToken)
        where T : class
    {
        var queueLength = await db.ListLengthAsync(queueKey);
        if (queueLength == 0)
            return;

        var processedCount = 0;
        var failedCount = 0;
        var batchSize = Math.Min((int)queueLength, MaxBatchSize);

        _logger.LogDebug("Processing {Count} items from {Queue}", batchSize, queueKey);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        for (var i = 0; i < batchSize; i++)
        {
            var item = await db.ListLeftPopAsync(queueKey);
            if (!item.HasValue)
                break;

            try
            {
                var auditLog = JsonSerializer.Deserialize<T>((string)item!, JsonOptions);
                if (auditLog != null)
                {
                    dbContext.Set<T>().Add(auditLog);
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize audit item from retry queue");
                failedCount++;
            }
        }

        if (processedCount > 0)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Audit retry: processed {Processed}, failed {Failed} from {Queue}",
                    processedCount, failedCount, queueKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save retried audit records — they will be lost");
            }
        }
    }
}
