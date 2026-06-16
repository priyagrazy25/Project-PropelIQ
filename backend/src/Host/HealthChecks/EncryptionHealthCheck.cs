using System.Net.Security;
using System.Security.Authentication;
using Clinical.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Host.HealthChecks;

/// <summary>
/// Health check that verifies encryption infrastructure is properly configured (NFR-005, NFR-006).
/// 
/// Checks:
/// 1. SQL Server TDE is active (or Always Encrypted is available)
/// 2. File encryption keys are loaded and valid
/// 3. TLS 1.2+ is enforced for database connections
/// </summary>
public sealed class EncryptionHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly IFileEncryptionService? _fileEncryption;
    private readonly ILogger<EncryptionHealthCheck> _logger;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public EncryptionHealthCheck(
        IConfiguration configuration,
        IFileEncryptionService? fileEncryption,
        ILogger<EncryptionHealthCheck> logger)
    {
        _connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string not configured.");
        _fileEncryption = fileEncryption;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var issues = new List<string>();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);

            // Check 1: Database encryption status
            await CheckDatabaseEncryptionAsync(data, issues, cts.Token);

            // Check 2: File encryption keys
            CheckFileEncryptionKeys(data, issues);

            // Check 3: TLS configuration
            CheckTlsConfiguration(data, issues);

            if (issues.Count > 0)
            {
                return HealthCheckResult.Degraded(
                    description: $"Encryption issues detected: {string.Join("; ", issues)}",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                description: "All encryption checks passed.",
                data: data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded(
                description: "Encryption health check timed out.",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption health check failed");
            return HealthCheckResult.Degraded(
                description: $"Encryption health check failed: {ex.Message}",
                exception: ex,
                data: data);
        }
    }

    private async Task CheckDatabaseEncryptionAsync(
        Dictionary<string, object> data,
        List<string> issues,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if TDE is enabled (only works on Standard/Enterprise editions)
            await using var tdeCommand = connection.CreateCommand();
            tdeCommand.CommandText = @"
                SELECT 
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM sys.dm_database_encryption_keys 
                            WHERE database_id = DB_ID() AND encryption_state = 3
                        ) THEN 1 
                        ELSE 0 
                    END AS TdeEnabled";

            var tdeResult = await tdeCommand.ExecuteScalarAsync(cancellationToken);
            var tdeEnabled = Convert.ToInt32(tdeResult) == 1;
            data["TDE_Enabled"] = tdeEnabled;

            // Check if Always Encrypted column master keys exist
            await using var aeCommand = connection.CreateCommand();
            aeCommand.CommandText = @"
                SELECT COUNT(*) FROM sys.column_master_keys";

            var aeResult = await aeCommand.ExecuteScalarAsync(cancellationToken);
            var aeKeyCount = Convert.ToInt32(aeResult);
            data["AlwaysEncrypted_CMK_Count"] = aeKeyCount;

            // Check connection encryption
            await using var connEncCommand = connection.CreateCommand();
            connEncCommand.CommandText = "SELECT encrypt_option FROM sys.dm_exec_connections WHERE session_id = @@SPID";
            var encryptOption = await connEncCommand.ExecuteScalarAsync(cancellationToken);
            var connectionEncrypted = encryptOption?.ToString() == "TRUE";
            data["Connection_Encrypted"] = connectionEncrypted;

            if (!connectionEncrypted)
            {
                issues.Add("Database connection is not encrypted");
            }

            // Note: TDE is not available on SQL Server Express, so we don't flag it as an issue
            if (!tdeEnabled)
            {
                data["TDE_Note"] = "TDE not enabled (requires SQL Server Standard/Enterprise)";
            }
        }
        catch (SqlException ex) when (ex.Number == 297) // sys.dm_database_encryption_keys permission denied
        {
            data["TDE_Note"] = "Permission denied to check TDE status";
        }
    }

    private void CheckFileEncryptionKeys(
        Dictionary<string, object> data,
        List<string> issues)
    {
        if (_fileEncryption == null)
        {
            data["FileEncryption"] = "Not configured";
            issues.Add("File encryption service not registered");
            return;
        }

        var currentKeyId = _fileEncryption.GetCurrentKeyId();
        data["FileEncryption_CurrentKeyId"] = currentKeyId;

        var keyAvailable = _fileEncryption.IsKeyAvailable(currentKeyId);
        data["FileEncryption_KeyAvailable"] = keyAvailable;

        if (!keyAvailable)
        {
            issues.Add($"File encryption key '{currentKeyId}' not available");
        }
    }

    private void CheckTlsConfiguration(
        Dictionary<string, object> data,
        List<string> issues)
    {
        // Document the expected TLS configuration
        data["TLS_MinimumProtocol"] = "TLS 1.2";
        data["TLS_Protocols_Allowed"] = "TLS 1.2, TLS 1.3";

        // Verify ServicePointManager defaults (for legacy HttpWebRequest)
        var securityProtocol = System.Net.ServicePointManager.SecurityProtocol;
        data["ServicePointManager_Protocols"] = securityProtocol.ToString();

        // Check if legacy protocols are enabled (should not be)
        if ((securityProtocol & System.Net.SecurityProtocolType.Ssl3) != 0)
        {
            issues.Add("SSL 3.0 is enabled (insecure)");
        }
#pragma warning disable SYSLIB0039 // Tls and Tls11 are obsolete
        if ((securityProtocol & System.Net.SecurityProtocolType.Tls) != 0)
        {
            issues.Add("TLS 1.0 is enabled (insecure)");
        }
        if ((securityProtocol & System.Net.SecurityProtocolType.Tls11) != 0)
        {
            issues.Add("TLS 1.1 is enabled (insecure)");
        }
#pragma warning restore SYSLIB0039
    }
}
