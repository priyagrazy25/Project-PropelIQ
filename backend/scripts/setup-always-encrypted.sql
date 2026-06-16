-- Setup Always Encrypted for PHI columns
-- SQL Server Express 2022 / 2025
-- HIPAA Requirements: NFR-005, FR-030 - Column-level encryption for sensitive data
--
-- Run this script against the UnifiedPatientAccess database with db_owner privileges.
-- Requires: SSMS with Always Encrypted enabled OR Azure Key Vault for key management.

USE UnifiedPatientAccess;
GO

-- ==================================================================
-- Step 1: Create Column Master Key (CMK)
-- The CMK protects the Column Encryption Keys (CEKs)
-- In production, use Azure Key Vault or Windows Certificate Store
-- ==================================================================

-- For development: Create a certificate in the Current User store
-- In production: Use Azure Key Vault with KEY_PATH = 'https://vault.azure.net/keys/...'

IF NOT EXISTS (SELECT * FROM sys.column_master_keys WHERE name = 'CMK_UnifiedPatientAccess')
BEGIN
    CREATE COLUMN MASTER KEY CMK_UnifiedPatientAccess
    WITH (
        KEY_STORE_PROVIDER_NAME = 'MSSQL_CERTIFICATE_STORE',
        KEY_PATH = 'CurrentUser/My/UnifiedPatientAccessCMK'
    );
    PRINT 'Column Master Key created.';
END
ELSE
    PRINT 'Column Master Key already exists.';
GO

-- ==================================================================
-- Step 2: Create Column Encryption Key (CEK)
-- The CEK encrypts the actual column data using AES-256
-- ==================================================================

IF NOT EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = 'CEK_UnifiedPatientAccess')
BEGIN
    CREATE COLUMN ENCRYPTION KEY CEK_UnifiedPatientAccess
    WITH VALUES (
        COLUMN_MASTER_KEY = CMK_UnifiedPatientAccess,
        ALGORITHM = 'RSA_OAEP',
        ENCRYPTED_VALUE = 0x -- Placeholder: SSMS or Key Vault generates this automatically
    );
    PRINT 'Column Encryption Key created.';
END
ELSE
    PRINT 'Column Encryption Key already exists.';
GO

-- ==================================================================
-- Step 3: Apply Always Encrypted to Sensitive Columns
-- 
-- Deterministic encryption: Allows equality comparisons (lookups)
-- Randomized encryption: More secure, no equality comparisons
--
-- IMPORTANT: These ALTER statements require:
-- 1. SSMS with "Enable Always Encrypted" in connection settings
-- 2. Or use SqlPackage.exe with /p:ColumnEncryptionSettings
-- ==================================================================

-- Identity module: PasswordHash (deterministic for login lookups)
-- Note: The application uses BCrypt hashing, so column encryption adds defense-in-depth
/*
ALTER TABLE [Identity].[Users]
ALTER COLUMN [PasswordHash] NVARCHAR(256)
COLLATE Latin1_General_BIN2
ENCRYPTED WITH (
    COLUMN_ENCRYPTION_KEY = CEK_UnifiedPatientAccess,
    ENCRYPTION_TYPE = DETERMINISTIC,
    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
) NOT NULL;
GO
*/

-- Clinical module: ClinicalNotes (randomized - no lookups needed)
/*
ALTER TABLE [Clinical].[EncounterNotes]
ALTER COLUMN [Notes] NVARCHAR(MAX)
ENCRYPTED WITH (
    COLUMN_ENCRYPTION_KEY = CEK_UnifiedPatientAccess,
    ENCRYPTION_TYPE = RANDOMIZED,
    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
) NULL;
GO
*/

-- Identity module: SocialSecurityNumber (deterministic for lookups)
/*
ALTER TABLE [Identity].[Patients]
ALTER COLUMN [SocialSecurityNumber] NVARCHAR(11)
COLLATE Latin1_General_BIN2
ENCRYPTED WITH (
    COLUMN_ENCRYPTION_KEY = CEK_UnifiedPatientAccess,
    ENCRYPTION_TYPE = DETERMINISTIC,
    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
) NULL;
GO
*/

-- Identity module: InsurancePolicyNumber (deterministic for lookups)
/*
ALTER TABLE [Identity].[Patients]
ALTER COLUMN [InsurancePolicyNumber] NVARCHAR(50)
COLLATE Latin1_General_BIN2
ENCRYPTED WITH (
    COLUMN_ENCRYPTION_KEY = CEK_UnifiedPatientAccess,
    ENCRYPTION_TYPE = DETERMINISTIC,
    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
) NULL;
GO
*/

-- Identity module: InsuranceGroupNumber (deterministic for lookups)
/*
ALTER TABLE [Identity].[Patients]
ALTER COLUMN [InsuranceGroupNumber] NVARCHAR(50)
COLLATE Latin1_General_BIN2
ENCRYPTED WITH (
    COLUMN_ENCRYPTION_KEY = CEK_UnifiedPatientAccess,
    ENCRYPTION_TYPE = DETERMINISTIC,
    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
) NULL;
GO
*/

-- ==================================================================
-- Step 4: Verify Always Encrypted Configuration
-- ==================================================================

SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    cek.name AS EncryptionKeyName,
    c.encryption_type_desc AS EncryptionType
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.column_encryption_keys cek ON c.column_encryption_key_id = cek.column_encryption_key_id
WHERE c.encryption_type IS NOT NULL
ORDER BY t.name, c.name;
GO

PRINT 'Always Encrypted configuration complete.';
PRINT 'NOTE: Column ALTER statements are commented out. Run them via SSMS with Always Encrypted enabled.';
GO
