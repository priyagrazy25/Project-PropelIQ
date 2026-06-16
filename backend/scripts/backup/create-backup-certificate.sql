/*
 * create-backup-certificate.sql
 * Creates a database master key and AES-256 backup encryption certificate
 * for the UnifiedPatientAccess database per DR-015.
 *
 * Run once on the SQL Server instance with sysadmin privileges.
 * IMPORTANT: Back up the certificate and private key to a secure location
 * immediately after creation — they are required for restore.
 */

USE master;
GO

-- Create a Database Master Key if one does not already exist
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = '$(MasterKeyPassword)';
    PRINT 'Database Master Key created.';
END
ELSE
    PRINT 'Database Master Key already exists.';
GO

-- Create the backup encryption certificate
IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE name = 'UnifiedPatientAccess_BackupCert')
BEGIN
    CREATE CERTIFICATE UnifiedPatientAccess_BackupCert
        WITH SUBJECT = 'UnifiedPatientAccess Database Backup Encryption Certificate',
        EXPIRY_DATE = '2030-12-31';
    PRINT 'Backup encryption certificate created.';
END
ELSE
    PRINT 'Backup encryption certificate already exists.';
GO

-- Export certificate and private key for disaster recovery
-- Store these files in a separate secure location from the database backups
BACKUP CERTIFICATE UnifiedPatientAccess_BackupCert
    TO FILE = '$(BackupCertPath)\UnifiedPatientAccess_BackupCert.cer'
    WITH PRIVATE KEY (
        FILE = '$(BackupCertPath)\UnifiedPatientAccess_BackupCert.pvk',
        ENCRYPTION BY PASSWORD = '$(CertPrivateKeyPassword)'
    );
GO

PRINT 'Certificate and private key exported. Store in a secure location separate from backups.';
GO
