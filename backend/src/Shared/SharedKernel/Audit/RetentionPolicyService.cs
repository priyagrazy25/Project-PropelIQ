using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;

namespace SharedKernel.Audit;

/// <summary>
/// Background service that enforces audit record retention policy (DR-011).
/// Archives records older than retention period to cold storage — never deletes.
/// Minimum retention: 7 years per HIPAA requirements.
/// </summary>
public sealed class RetentionPolicyService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionPolicyService> _logger;
    private readonly RetentionPolicyOptions _options;

    // Run daily at 2 AM
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public RetentionPolicyService(
        IServiceProvider serviceProvider,
        IOptions<RetentionPolicyOptions> options,
        ILogger<RetentionPolicyService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RetentionPolicyService started — retention period: {Years} years, archive path: {Path}",
            _options.RetentionYears,
            _options.ArchivePath);

        // Wait for initial delay (avoid startup overhead)
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRetentionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during retention policy processing");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("RetentionPolicyService stopped");
    }

    private async Task ProcessRetentionAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-_options.RetentionYears);

        _logger.LogDebug("Processing audit records older than {CutoffDate}", cutoffDate);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        // Count records eligible for archival (but don't delete!)
        var auditLogsToArchive = await dbContext.Set<AuditLog>()
            .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
            .CountAsync(cancellationToken);

        var aiAuditLogsToArchive = await dbContext.Set<AiInvocationAuditLog>()
            .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
            .CountAsync(cancellationToken);

        if (auditLogsToArchive == 0 && aiAuditLogsToArchive == 0)
        {
            _logger.LogDebug("No audit records eligible for archival");
            return;
        }

        _logger.LogInformation(
            "Found {AuditLogs} audit logs and {AiAuditLogs} AI audit logs eligible for archival",
            auditLogsToArchive, aiAuditLogsToArchive);

        // Archive in batches
        await ArchiveAuditLogsAsync(dbContext, cutoffDate, cancellationToken);
        await ArchiveAiAuditLogsAsync(dbContext, cutoffDate, cancellationToken);
    }

    private async Task ArchiveAuditLogsAsync(DbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        const int batchSize = 1000;
        var archived = 0;

        while (true)
        {
            var batch = await dbContext.Set<AuditLog>()
                .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
                break;

            // Write to archive file (compressed JSON lines)
            await WriteToArchiveAsync(batch, "audit-logs", cancellationToken);

            // Mark as archived (NOT deleted per DR-011)
            foreach (var log in batch)
            {
                log.IsArchived = true;
                log.ArchivedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            archived += batch.Count;

            _logger.LogDebug("Archived {Count} audit logs", batch.Count);
        }

        if (archived > 0)
        {
            _logger.LogInformation("Total archived audit logs: {Count}", archived);
        }
    }

    private async Task ArchiveAiAuditLogsAsync(DbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
    {
        const int batchSize = 1000;
        var archived = 0;

        while (true)
        {
            var batch = await dbContext.Set<AiInvocationAuditLog>()
                .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
                break;

            // Write to archive file
            await WriteToArchiveAsync(batch, "ai-audit-logs", cancellationToken);

            // Mark as archived
            foreach (var log in batch)
            {
                log.IsArchived = true;
                log.ArchivedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            archived += batch.Count;

            _logger.LogDebug("Archived {Count} AI audit logs", batch.Count);
        }

        if (archived > 0)
        {
            _logger.LogInformation("Total archived AI audit logs: {Count}", archived);
        }
    }

    private async Task WriteToArchiveAsync<T>(IEnumerable<T> records, string prefix, CancellationToken cancellationToken)
    {
        var archivePath = _options.ArchivePath;
        if (string.IsNullOrEmpty(archivePath))
        {
            _logger.LogWarning("Archive path not configured — skipping file write");
            return;
        }

        Directory.CreateDirectory(archivePath);

        var fileName = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jsonl.gz";
        var filePath = Path.Combine(archivePath, fileName);

        await using var fileStream = File.Create(filePath);
        await using var gzipStream = new System.IO.Compression.GZipStream(
            fileStream,
            System.IO.Compression.CompressionLevel.Optimal);
        await using var writer = new StreamWriter(gzipStream);

        foreach (var record in records)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(record);
            await writer.WriteLineAsync(json);
        }

        _logger.LogDebug("Wrote archive file: {FilePath}", filePath);
    }
}

/// <summary>
/// Configuration options for audit retention policy.
/// </summary>
public sealed class RetentionPolicyOptions
{
    /// <summary>
    /// Number of years to retain audit records before archival.
    /// Minimum: 7 years per HIPAA DR-011.
    /// </summary>
    public int RetentionYears { get; set; } = 7;

    /// <summary>
    /// Path to cold storage for archived audit records.
    /// </summary>
    public string ArchivePath { get; set; } = "./audit-archives";
}
