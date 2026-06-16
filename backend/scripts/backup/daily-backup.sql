/*
 * daily-backup.sql
 * Automated daily full backup with AES-256 encryption per DR-014, DR-015.
 * RPO: 24 hours (daily full backup schedule).
 *
 * Schedule via SQL Server Agent (Standard+) or Windows Task Scheduler (Express):
 *   sqlcmd -S localhost\SQLEXPRESS -E -i daily-backup.sql
 *       -v BackupDir="D:\Backups\UnifiedPatientAccess"
 *       -v RetentionDays="30"
 *
 * Backup storage MUST be on a separate drive/location from the primary database (DR-015).
 */

SET NOCOUNT ON;
GO

DECLARE @BackupDir   NVARCHAR(500) = '$(BackupDir)';
DECLARE @Retention   INT           = $(RetentionDays);
DECLARE @DbName      NVARCHAR(128) = N'UnifiedPatientAccess';
DECLARE @Timestamp   NVARCHAR(20)  = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
DECLARE @BackupFile  NVARCHAR(600) = @BackupDir + N'\' + @DbName + N'_' + @Timestamp + N'.bak';
DECLARE @Description NVARCHAR(256) = N'Daily full encrypted backup - ' + @Timestamp;

-- Ensure backup directory exists (xp_create_subdir is idempotent)
EXEC master.dbo.xp_create_subdir @BackupDir;

-- Perform AES-256 encrypted full backup with checksum verification
BACKUP DATABASE @DbName
    TO DISK = @BackupFile
    WITH
        FORMAT,
        INIT,
        NAME        = @Description,
        COMPRESSION,
        ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = UnifiedPatientAccess_BackupCert),
        CHECKSUM,
        STATS       = 10;

PRINT 'Backup completed: ' + @BackupFile;
GO

-- Verify backup integrity
DECLARE @BackupDir2  NVARCHAR(500) = '$(BackupDir)';
DECLARE @Timestamp2  NVARCHAR(20)  = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
DECLARE @VerifyFile  NVARCHAR(600) = @BackupDir2 + N'\UnifiedPatientAccess_' + @Timestamp2 + N'.bak';

RESTORE VERIFYONLY
    FROM DISK = @VerifyFile
    WITH CHECKSUM;

PRINT 'Backup verification passed.';
GO

-- Rotate old backups beyond retention period
DECLARE @RetentionDays INT = $(RetentionDays);
DECLARE @CutoffDate DATETIME = DATEADD(DAY, -@RetentionDays, GETDATE());
DECLARE @OldFile NVARCHAR(600);

DECLARE @BackupHistory TABLE (BackupFile NVARCHAR(600), BackupDate DATETIME);

INSERT INTO @BackupHistory (BackupFile, BackupDate)
SELECT
    bmf.physical_device_name,
    bs.backup_finish_date
FROM msdb.dbo.backupset bs
JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
WHERE bs.database_name = N'UnifiedPatientAccess'
  AND bs.type = 'D'
  AND bs.backup_finish_date < @CutoffDate;

DECLARE cleanup_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT BackupFile FROM @BackupHistory;

OPEN cleanup_cursor;
FETCH NEXT FROM cleanup_cursor INTO @OldFile;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC master.dbo.xp_delete_file 0, @OldFile;
    PRINT 'Rotated old backup: ' + @OldFile;
    FETCH NEXT FROM cleanup_cursor INTO @OldFile;
END

CLOSE cleanup_cursor;
DEALLOCATE cleanup_cursor;

PRINT 'Backup rotation complete.';
GO
