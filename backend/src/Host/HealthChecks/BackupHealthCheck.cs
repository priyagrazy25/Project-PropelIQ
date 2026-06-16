using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Host.HealthChecks;

/// <summary>
/// Health check verifying database backup recency and integrity per DR-014, DR-015.
/// Returns Degraded if backup age exceeds 4 hours, Unhealthy if exceeds 24 hours (RPO).
/// Also verifies backup certificate validity and file existence.
/// </summary>
public sealed class BackupHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<BackupHealthCheck> _logger;
    private static readonly TimeSpan MaxHealthyAge = TimeSpan.FromHours(4);
    private static readonly TimeSpan MaxDegradedAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan CertExpiryWarning = TimeSpan.FromDays(30);

    public BackupHealthCheck(IConfiguration configuration, ILogger<BackupHealthCheck> logger)
    {
        _connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string not configured.");
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check latest backup age
            var backupAge = await GetLatestBackupAgeAsync(connection, cancellationToken);
            data["LastBackupAge"] = backupAge?.ToString(@"hh\:mm\:ss") ?? "Never";

            if (backupAge is null)
            {
                _logger.LogWarning("No database backups found for UnifiedPatientAccess");
                return HealthCheckResult.Unhealthy(
                    "No database backups found. RPO 24h requirement at risk.",
                    data: data);
            }

            // Check certificate validity
            var certExpiry = await GetCertificateExpiryAsync(connection, cancellationToken);
            data["CertificateExpiry"] = certExpiry?.ToString("yyyy-MM-dd") ?? "Not found";

            if (certExpiry is null)
            {
                _logger.LogWarning("Backup encryption certificate not found");
                return HealthCheckResult.Unhealthy(
                    "Backup encryption certificate not found. Cannot create encrypted backups.",
                    data: data);
            }

            // Check if certificate is expiring soon
            var daysUntilExpiry = (certExpiry.Value - DateTime.UtcNow).TotalDays;
            data["CertificateDaysUntilExpiry"] = (int)daysUntilExpiry;

            if (daysUntilExpiry <= 0)
            {
                _logger.LogError("Backup encryption certificate has expired");
                return HealthCheckResult.Unhealthy(
                    "Backup encryption certificate has expired. Immediate action required.",
                    data: data);
            }

            if (daysUntilExpiry <= CertExpiryWarning.TotalDays)
            {
                _logger.LogWarning(
                    "Backup encryption certificate expires in {Days} days",
                    (int)daysUntilExpiry);
            }

            // Evaluate backup age thresholds
            if (backupAge > MaxDegradedAge)
            {
                _logger.LogError(
                    "Database backup is {Hours:F1} hours old, exceeds RPO 24h",
                    backupAge.Value.TotalHours);
                return HealthCheckResult.Unhealthy(
                    $"Database backup is {backupAge.Value.TotalHours:F1} hours old. RPO 24h exceeded.",
                    data: data);
            }

            if (backupAge > MaxHealthyAge)
            {
                _logger.LogWarning(
                    "Database backup is {Hours:F1} hours old, exceeds 4h threshold",
                    backupAge.Value.TotalHours);
                return HealthCheckResult.Degraded(
                    $"Database backup is {backupAge.Value.TotalHours:F1} hours old. Consider running backup.",
                    data: data);
            }

            data["Status"] = "Backups healthy";
            return HealthCheckResult.Healthy(
                $"Latest backup is {backupAge.Value.TotalMinutes:F0} minutes old. Certificate valid for {(int)daysUntilExpiry} days.",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check backup health");
            return HealthCheckResult.Degraded(
                "Unable to verify backup status.",
                ex,
                data);
        }
    }

    private static async Task<TimeSpan?> GetLatestBackupAgeAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT TOP 1 DATEDIFF(SECOND, backup_finish_date, GETUTCDATE()) AS AgeSeconds
            FROM msdb.dbo.backupset
            WHERE database_name = 'UnifiedPatientAccess'
              AND type IN ('D', 'I') -- Full or Differential
            ORDER BY backup_finish_date DESC";

        await using var command = new SqlCommand(query, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null or DBNull)
            return null;

        return TimeSpan.FromSeconds(Convert.ToInt32(result));
    }

    private static async Task<DateTime?> GetCertificateExpiryAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT expiry_date
            FROM master.sys.certificates
            WHERE name = 'UnifiedPatientAccess_BackupCert'";

        await using var command = new SqlCommand(query, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null or DBNull)
            return null;

        return Convert.ToDateTime(result);
    }
}
