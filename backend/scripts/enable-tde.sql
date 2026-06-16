-- Enable Transparent Data Encryption (TDE) for UnifiedPatientAccess database
-- SQL Server Express 2022 / 2025
-- HIPAA Requirement: NFR-005 - AES-256 encryption at rest
--
-- NOTE: TDE is NOT available on SQL Server Express edition.
-- This script is provided for production deployment on Standard/Enterprise editions.
-- For Express development, column-level encryption (Always Encrypted) is the alternative.
-- Run this script against the master database with sysadmin privileges.

USE master;
GO

-- Step 1: Create a master key in the master database (if not exists)
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = '$(MasterKeyPassword)';
    PRINT 'Master key created.';
END
ELSE
    PRINT 'Master key already exists.';
GO

-- Step 2: Create a certificate for TDE
IF NOT EXISTS (SELECT * FROM sys.certificates WHERE name = 'UnifiedPatientAccess_TDE_Cert')
BEGIN
    CREATE CERTIFICATE UnifiedPatientAccess_TDE_Cert
        WITH SUBJECT = 'TDE Certificate for UnifiedPatientAccess';
    PRINT 'TDE certificate created.';
END
ELSE
    PRINT 'TDE certificate already exists.';
GO

-- Step 3: Back up the certificate (CRITICAL - store securely)
-- BACKUP CERTIFICATE UnifiedPatientAccess_TDE_Cert
--     TO FILE = '$(BackupPath)\UnifiedPatientAccess_TDE_Cert.cer'
--     WITH PRIVATE KEY (
--         FILE = '$(BackupPath)\UnifiedPatientAccess_TDE_Cert.pvk',
--         ENCRYPTION BY PASSWORD = '$(CertBackupPassword)'
--     );
-- GO

-- Step 4: Create database encryption key and enable TDE
USE UnifiedPatientAccess;
GO

IF NOT EXISTS (SELECT * FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID())
BEGIN
    CREATE DATABASE ENCRYPTION KEY
        WITH ALGORITHM = AES_256
        ENCRYPTION BY SERVER CERTIFICATE UnifiedPatientAccess_TDE_Cert;
    PRINT 'Database encryption key created with AES-256.';
END
ELSE
    PRINT 'Database encryption key already exists.';
GO

ALTER DATABASE UnifiedPatientAccess SET ENCRYPTION ON;
GO

-- Step 5: Verify TDE is active
SELECT
    db.name AS DatabaseName,
    dek.encryption_state AS EncryptionState,
    dek.key_algorithm AS Algorithm,
    dek.key_length AS KeyLength,
    CASE dek.encryption_state
        WHEN 0 THEN 'No encryption key'
        WHEN 1 THEN 'Unencrypted'
        WHEN 2 THEN 'Encryption in progress'
        WHEN 3 THEN 'Encrypted'
        WHEN 4 THEN 'Key change in progress'
        WHEN 5 THEN 'Decryption in progress'
        WHEN 6 THEN 'Protection change in progress'
    END AS EncryptionStateDescription
FROM sys.dm_database_encryption_keys dek
JOIN sys.databases db ON dek.database_id = db.database_id
WHERE db.name = 'UnifiedPatientAccess';
GO
