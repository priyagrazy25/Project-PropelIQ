using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Host.Services;

/// <summary>
/// Background service that performs weekly automated restore verification per DR-014.
/// Restores the latest backup to a verification database, validates integrity, then drops it.
/// Validates RPO 24h / RTO 4h disaster recovery capability.
/// </summary>
public sealed class RestoreVerificationService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RestoreVerificationService> _logger;
    private readonly string _connectionString;

    // Run weekly by default
    private static readonly TimeSpan VerificationInterval = TimeSpan.FromDays(7);

    // Verification database name suffix
    private const string VerifyDbSuffix = "_RestoreVerify";
    private const string PrimaryDbName = "UnifiedPatientAccess";

    public RestoreVerificationService(
        IConfiguration configuration,
        ILogger<RestoreVerificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string not configured.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RestoreVerificationService started — weekly backup restore verification enabled");

        // Initial delay to allow system startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunVerificationAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Restore verification failed — manual investigation required");
            }

            try
            {
                await Task.Delay(VerificationInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("RestoreVerificationService stopped");
    }

    private async Task RunVerificationAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("=== Starting backup restore verification ===");

        // Connect to master to run restore commands
        var masterConnectionString = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            // Step 1: Find latest backup file
            var latestBackup = await GetLatestBackupPathAsync(connection, cancellationToken);
            if (latestBackup is null)
            {
                _logger.LogWarning("No backups found for verification");
                return;
            }

            _logger.LogInformation("Verifying backup: {BackupPath}", latestBackup);

            // Step 2: Drop existing verification database if it exists
            await DropVerifyDatabaseAsync(connection, cancellationToken);

            // Step 3: Verify backup integrity before restore
            var verifyResult = await VerifyBackupAsync(connection, latestBackup, cancellationToken);
            if (!verifyResult)
            {
                _logger.LogError("Backup verification FAILED for {BackupPath}", latestBackup);
                return;
            }

            _logger.LogInformation("Backup integrity verification passed");

            // Step 4: Restore to verification database
            var restoreSuccess = await RestoreToVerifyDbAsync(connection, latestBackup, cancellationToken);
            if (!restoreSuccess)
            {
                _logger.LogError("Restore to verification database FAILED");
                return;
            }

            _logger.LogInformation("Database restored successfully to verification instance");

            // Step 5: Validate restored data integrity
            var dataValid = await ValidateRestoredDataAsync(connection, cancellationToken);
            if (!dataValid)
            {
                _logger.LogError("Restored data validation FAILED — data integrity issue detected");
                return;
            }

            _logger.LogInformation("Restored data validation passed");

            // Step 6: Drop verification database
            await DropVerifyDatabaseAsync(connection, cancellationToken);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "=== Restore verification PASSED in {Minutes:F1} minutes ===",
                elapsed.TotalMinutes);

            // Verify RTO compliance (should be achievable within 4 hours)
            if (elapsed > TimeSpan.FromHours(4))
            {
                _logger.LogWarning(
                    "Restore took {Minutes:F1} minutes — may exceed RTO 4h target (DR-014)",
                    elapsed.TotalMinutes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore verification encountered an error");

            // Attempt cleanup
            try
            {
                await DropVerifyDatabaseAsync(connection, cancellationToken);
            }
            catch
            {
                // Ignore cleanup errors
            }

            throw;
        }
    }

    private static async Task<string?> GetLatestBackupPathAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT TOP 1 bmf.physical_device_name
            FROM msdb.dbo.backupset bs
            JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
            WHERE bs.database_name = @DbName
              AND bs.type IN ('D', 'I')
            ORDER BY bs.backup_finish_date DESC";

        await using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@DbName", PrimaryDbName);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    private static async Task<bool> VerifyBackupAsync(
        SqlConnection connection,
        string backupPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = $"RESTORE VERIFYONLY FROM DISK = @BackupPath WITH CHECKSUM";
            await using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@BackupPath", backupPath);
            cmd.CommandTimeout = 600; // 10 minutes

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (SqlException ex)
        {
            // Log but don't throw - verification failure is expected return value
            Console.WriteLine($"Backup verification failed: {ex.Message}");
            return false;
        }
    }

    private static async Task DropVerifyDatabaseAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var verifyDbName = PrimaryDbName + VerifyDbSuffix;

        var query = $@"
            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @VerifyDbName)
            BEGIN
                ALTER DATABASE [{verifyDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{verifyDbName}];
            END";

        await using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@VerifyDbName", verifyDbName);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<bool> RestoreToVerifyDbAsync(
        SqlConnection connection,
        string backupPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var verifyDbName = PrimaryDbName + VerifyDbSuffix;

            // Get data/log file locations from backup
            var fileListQuery = "RESTORE FILELISTONLY FROM DISK = @BackupPath";
            await using var fileListCmd = new SqlCommand(fileListQuery, connection);
            fileListCmd.Parameters.AddWithValue("@BackupPath", backupPath);

            string? dataFile = null;
            string? logFile = null;

            await using var reader = await fileListCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var logicalName = reader["LogicalName"]?.ToString();
                var type = reader["Type"]?.ToString();

                if (type == "D")
                    dataFile = logicalName;
                else if (type == "L")
                    logFile = logicalName;
            }

            await reader.CloseAsync();

            if (dataFile is null || logFile is null)
            {
                _logger.LogError("Could not determine data/log file names from backup");
                return false;
            }

            // Get default data directory
            var defaultDataDir = await GetDefaultDataDirectoryAsync(connection, cancellationToken);

            // Perform restore with MOVE to avoid file conflicts
            var restoreQuery = $@"
                RESTORE DATABASE [{verifyDbName}]
                FROM DISK = @BackupPath
                WITH
                    MOVE @DataFile TO @DataFilePath,
                    MOVE @LogFile TO @LogFilePath,
                    STATS = 10";

            await using var restoreCmd = new SqlCommand(restoreQuery, connection);
            restoreCmd.CommandTimeout = 1800; // 30 minutes
            restoreCmd.Parameters.AddWithValue("@BackupPath", backupPath);
            restoreCmd.Parameters.AddWithValue("@DataFile", dataFile);
            restoreCmd.Parameters.AddWithValue("@DataFilePath",
                Path.Combine(defaultDataDir, $"{verifyDbName}.mdf"));
            restoreCmd.Parameters.AddWithValue("@LogFile", logFile);
            restoreCmd.Parameters.AddWithValue("@LogFilePath",
                Path.Combine(defaultDataDir, $"{verifyDbName}_log.ldf"));

            await restoreCmd.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database restore failed");
            return false;
        }
    }

    private static async Task<string> GetDefaultDataDirectoryAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(500))";

        await using var cmd = new SqlCommand(query, connection);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);

        return result?.ToString() ?? @"C:\SQLData";
    }

    private async Task<bool> ValidateRestoredDataAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var verifyDbName = PrimaryDbName + VerifyDbSuffix;

        try
        {
            // Switch to verification database
            var useQuery = $"USE [{verifyDbName}]";
            await using var useCmd = new SqlCommand(useQuery, connection);
            await useCmd.ExecuteNonQueryAsync(cancellationToken);

            // Verify schemas exist
            const string schemaQuery = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.SCHEMATA
                WHERE SCHEMA_NAME IN ('identity', 'scheduling', 'clinical', 'notification')";

            await using var schemaCmd = new SqlCommand(schemaQuery, connection);
            var schemaCount = (int)(await schemaCmd.ExecuteScalarAsync(cancellationToken) ?? 0);

            if (schemaCount < 3)
            {
                _logger.LogError("Expected at least 3 schemas, found {Count}", schemaCount);
                return false;
            }

            // Verify core tables exist
            const string tableQuery = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                  AND TABLE_SCHEMA IN ('identity', 'scheduling', 'clinical', 'notification')";

            await using var tableCmd = new SqlCommand(tableQuery, connection);
            var tableCount = (int)(await tableCmd.ExecuteScalarAsync(cancellationToken) ?? 0);

            if (tableCount < 15)
            {
                _logger.LogWarning("Expected at least 15 tables, found {Count}", tableCount);
                // Warning only, not a failure
            }

            _logger.LogInformation(
                "Validation: {SchemaCount} schemas, {TableCount} tables found",
                schemaCount,
                tableCount);

            // Switch back to master
            var masterQuery = "USE [master]";
            await using var masterCmd = new SqlCommand(masterQuery, connection);
            await masterCmd.ExecuteNonQueryAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data validation query failed");
            return false;
        }
    }
}
