/*
 * restore-verify.sql
 * Database restore procedure with RTO verification per DR-014 (target: < 4 hours).
 *
 * Prerequisites:
 *   - Backup encryption certificate must be restored on the target server first:
 *       CREATE CERTIFICATE UnifiedPatientAccess_BackupCert
 *           FROM FILE = '<CertPath>\UnifiedPatientAccess_BackupCert.cer'
 *           WITH PRIVATE KEY (
 *               FILE = '<CertPath>\UnifiedPatientAccess_BackupCert.pvk',
 *               DECRYPTION BY PASSWORD = '<CertPrivateKeyPassword>'
 *           );
 *
 * Usage:
 *   sqlcmd -S localhost\SQLEXPRESS -E -i restore-verify.sql
 *       -v BackupFile="D:\Backups\UnifiedPatientAccess\UnifiedPatientAccess_20260419_020000.bak"
 *       -v RestoreDbName="UnifiedPatientAccess_Restored"
 *       -v DataDir="D:\SQLData"
 *       -v LogDir="D:\SQLLogs"
 */

SET NOCOUNT ON;
GO

DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
PRINT '=== Restore started at ' + CONVERT(NVARCHAR(30), @StartTime, 126) + ' ===';
GO

-- Step 1: Verify backup file header and encryption
RESTORE HEADERONLY FROM DISK = '$(BackupFile)';
GO

-- Step 2: Verify backup integrity before restore
RESTORE VERIFYONLY
    FROM DISK = '$(BackupFile)'
    WITH CHECKSUM;

PRINT 'Backup verification passed.';
GO

-- Step 3: Restore database to a new name (non-destructive)
RESTORE DATABASE [$(RestoreDbName)]
    FROM DISK = '$(BackupFile)'
    WITH
        MOVE N'UnifiedPatientAccess'     TO N'$(DataDir)\$(RestoreDbName).mdf',
        MOVE N'UnifiedPatientAccess_log' TO N'$(LogDir)\$(RestoreDbName)_log.ldf',
        REPLACE,
        STATS = 10;

PRINT 'Database restore completed.';
GO

-- Step 4: Verify restored data integrity
USE [$(RestoreDbName)];
GO

-- Verify all schemas exist
SELECT
    SCHEMA_NAME
FROM INFORMATION_SCHEMA.SCHEMATA
WHERE SCHEMA_NAME IN ('identity', 'scheduling', 'clinical')
ORDER BY SCHEMA_NAME;

-- Verify table count
DECLARE @TableCount INT;
SELECT @TableCount = COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_SCHEMA IN ('identity', 'scheduling', 'clinical');

PRINT 'Restored table count: ' + CAST(@TableCount AS NVARCHAR(10));

IF @TableCount < 15
    RAISERROR('WARNING: Expected at least 15 tables, found %d', 16, 1, @TableCount);
GO

-- Verify seed insurance data survived restore
DECLARE @InsuranceCount INT;
SELECT @InsuranceCount = COUNT(*)
FROM [identity].[InsurancePlans]
WHERE [IsDeleted] = 0;

PRINT 'Insurance plan records: ' + CAST(@InsuranceCount AS NVARCHAR(10));

IF @InsuranceCount = 0
    RAISERROR('WARNING: No insurance plan seed data found after restore.', 16, 1);
GO

-- Verify migration history is intact
SELECT MigrationId
FROM [identity].[__EFMigrationsHistory]
ORDER BY MigrationId;

SELECT MigrationId
FROM [scheduling].[__EFMigrationsHistory]
ORDER BY MigrationId;

SELECT MigrationId
FROM [clinical].[__EFMigrationsHistory]
ORDER BY MigrationId;
GO

-- Step 5: RTO timing
USE master;
GO

DECLARE @EndTime DATETIME2 = SYSUTCDATETIME();
DECLARE @StartTime2 DATETIME2;
SELECT TOP 1 @StartTime2 = restore_date
FROM msdb.dbo.restorehistory
WHERE destination_database_name = '$(RestoreDbName)'
ORDER BY restore_date DESC;

DECLARE @DurationMinutes INT = DATEDIFF(MINUTE, @StartTime2, @EndTime);

PRINT '=== Restore completed at ' + CONVERT(NVARCHAR(30), @EndTime, 126) + ' ===';
PRINT 'Total restore duration: ' + CAST(@DurationMinutes AS NVARCHAR(10)) + ' minutes';

IF @DurationMinutes > 240
    PRINT 'WARNING: Restore exceeded 4-hour RTO target!';
ELSE
    PRINT 'RTO verification PASSED (within 4-hour target).';
GO

-- Optional: Drop the restored verification database
-- DROP DATABASE [$(RestoreDbName)];
-- GO
