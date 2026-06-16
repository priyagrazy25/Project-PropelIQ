-- ============================================================================
-- Seed Script: Medical Code Verification Test Data
-- Purpose: Populate MedicalCodes table with sample ICD-10 and CPT codes for testing
-- Usage: Run after database migrations are applied
-- ============================================================================

USE [UnifiedPatientAccess]
GO

-- ============================================================================
-- Define test IDs
-- ============================================================================

-- First test patient
DECLARE @TestUserId UNIQUEIDENTIFIER = 'A1B2C3D4-E5F6-4A5B-8C7D-9E0F1A2B3C4D'
DECLARE @TestPatientId UNIQUEIDENTIFIER = 'B2C3D4E5-F6A7-5B6C-9D8E-0F1A2B3C4D5E'
DECLARE @TestDocumentId UNIQUEIDENTIFIER = 'C3D4E5F6-A7B8-6C7D-0E9F-1A2B3C4D5E6F'

-- Second test patient
DECLARE @TestUserId2 UNIQUEIDENTIFIER = 'D4E5F6A7-B8C9-5D6E-0F1A-2B3C4D5E6F7A'
DECLARE @TestPatientId2 UNIQUEIDENTIFIER = 'E5F6A7B8-C9D0-6E7F-1A2B-3C4D5E6F7A8B'
DECLARE @TestDocumentId2 UNIQUEIDENTIFIER = 'F6A7B8C9-D0E1-7F8A-2B3C-4D5E6F7A8B9C'

-- Staff user for verified codes
DECLARE @StaffUserId UNIQUEIDENTIFIER = '11111111-2222-3333-4444-555555555555'

-- ============================================================================
-- Create first test user, patient, and document (Jane Doe)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @TestUserId)
BEGIN
    INSERT INTO [identity].[Users] 
        ([Id], [Email], [PasswordHash], [FullName], [DateOfBirth], [ContactNumber], [Address], [Role], [Status], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES 
        (@TestUserId, 'jane.doe@test.com', '$2a$11$TestHashForDevelopmentOnly', 'Jane Elizabeth Doe', '1985-03-15', '(555) 123-4567', '123 Maple St, Springfield, IL 62701', 0, 1, GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created first test user: jane.doe@test.com'
END

IF NOT EXISTS (SELECT 1 FROM [identity].[Patients] WHERE [Id] = @TestPatientId)
BEGIN
    INSERT INTO [identity].[Patients]
        ([Id], [UserId], [InsuranceProvider], [InsurancePolicyNumber], [EmergencyContactName], [EmergencyContactPhone], [IntakeCompleted], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@TestPatientId, @TestUserId, 'BlueCross BlueShield', 'BCBS-12345678', 'John Doe', '(555) 987-6543', 1, GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created first test patient (Jane Doe)'
END

IF NOT EXISTS (SELECT 1 FROM [clinical].[ClinicalDocuments] WHERE [Id] = @TestDocumentId)
BEGIN
    INSERT INTO [clinical].[ClinicalDocuments]
        ([Id], [PatientId], [FileName], [EncryptedFilePath], [ContentType], [FileSizeBytes], [ProcessingStatus], [ProcessedAt], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@TestDocumentId, @TestPatientId, 'Medical_Records_Jane_Doe.pdf', '/encrypted/docs/test-doc-1.enc', 'application/pdf', 1024000, 'Completed', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created first test clinical document'
END

-- ============================================================================
-- Create second test user, patient, and document (John Smith)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @TestUserId2)
BEGIN
    INSERT INTO [identity].[Users] 
        ([Id], [Email], [PasswordHash], [FullName], [DateOfBirth], [ContactNumber], [Address], [Role], [Status], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES 
        (@TestUserId2, 'john.smith@test.com', '$2a$11$TestHashForDevelopmentOnly', 'John Robert Smith', '1978-07-22', '(555) 234-5678', '456 Oak Ave, Chicago, IL 60601', 0, 1, GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created second test user: john.smith@test.com'
END

IF NOT EXISTS (SELECT 1 FROM [identity].[Patients] WHERE [Id] = @TestPatientId2)
BEGIN
    INSERT INTO [identity].[Patients]
        ([Id], [UserId], [InsuranceProvider], [InsurancePolicyNumber], [EmergencyContactName], [EmergencyContactPhone], [IntakeCompleted], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@TestPatientId2, @TestUserId2, 'Aetna', 'AETNA-87654321', 'Sarah Smith', '(555) 876-5432', 1, GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created second test patient'
END

IF NOT EXISTS (SELECT 1 FROM [clinical].[ClinicalDocuments] WHERE [Id] = @TestDocumentId2)
BEGIN
    INSERT INTO [clinical].[ClinicalDocuments]
        ([Id], [PatientId], [FileName], [EncryptedFilePath], [ContentType], [FileSizeBytes], [ProcessingStatus], [ProcessedAt], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@TestDocumentId2, @TestPatientId2, 'Medical_History_2026-04-15.pdf', '/encrypted/docs/test-doc-2.enc', 'application/pdf', 2048000, 'Completed', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created second test clinical document'
END

-- Create staff user for verification if not exists
IF NOT EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @StaffUserId)
BEGIN
    INSERT INTO [identity].[Users] 
        ([Id], [Email], [PasswordHash], [FullName], [DateOfBirth], [ContactNumber], [Address], [Role], [Status], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES 
        (@StaffUserId, 'dr.martinez@clinic.com', '$2a$11$TestHashForDevelopmentOnly', 'Dr. Maria Martinez', '1975-01-15', '(555) 000-0001', '100 Medical Center Dr', 2, 1, GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created staff user: dr.martinez@clinic.com'
END

-- ============================================================================
-- Clear existing MedicalCode test data
-- ============================================================================

DELETE FROM [clinical].[MedicalCodes] WHERE [PatientId] IN (@TestPatientId, @TestPatientId2)
PRINT 'Cleared existing MedicalCode data for test patients'

-- ============================================================================
-- Seed MedicalCodes - ICD-10 Codes (CodeType = 'ICD10')
-- ============================================================================

-- Patient 1: Jane Doe - ICD-10 Codes
-- Pending codes (VerificationStatus = 'Pending')
INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'E11.65', 'Type 2 diabetes mellitus with hyperglycemia', 0.96, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'I10', 'Essential (primary) hypertension', 0.94, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -1, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'E78.5', 'Hyperlipidemia, unspecified', 0.91, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(MINUTE, -30, GETUTCDATE()), GETUTCDATE(), 0)

PRINT 'Inserted 3 pending ICD-10 codes for Jane Doe'

-- Verified codes (VerificationStatus = 'Verified')
INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'J06.9', 'Acute upper respiratory infection, unspecified', 0.92, 'Verified', @StaffUserId, DATEADD(DAY, -5, GETUTCDATE()), NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), 0),
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'M54.5', 'Low back pain', 0.88, 'Verified', @StaffUserId, DATEADD(DAY, -10, GETUTCDATE()), NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -12, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE()), 0)

PRINT 'Inserted 2 verified ICD-10 codes for Jane Doe'

-- Rejected code (VerificationStatus = 'Rejected')
INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'K21.0', 'Gastro-esophageal reflux disease with esophagitis', 0.52, 'Rejected', @StaffUserId, DATEADD(DAY, -3, GETUTCDATE()), 'Insufficient documentation to support diagnosis', 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), 0)

PRINT 'Inserted 1 rejected ICD-10 code for Jane Doe'

-- Patient 2: John Smith - ICD-10 Codes
INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'ICD10', 'J44.1', 'Chronic obstructive pulmonary disease with acute exacerbation', 0.89, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -4, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'ICD10', 'I25.10', 'Atherosclerotic heart disease of native coronary artery without angina pectoris', 0.85, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -3, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'ICD10', 'N18.3', 'Chronic kidney disease, stage 3', 0.78, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'ICD10', 'G47.33', 'Obstructive sleep apnea', 0.93, 'Verified', @StaffUserId, DATEADD(DAY, -7, GETUTCDATE()), NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), 0)

PRINT 'Inserted 4 ICD-10 codes for John Smith'

-- ============================================================================
-- Seed MedicalCodes - CPT Codes (CodeType = 'CPT')
-- ============================================================================

-- Patient 1: Jane Doe - CPT Codes
INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'CPT', '99213', 'Office or other outpatient visit for the evaluation and management of an established patient, moderate complexity', 0.87, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -1, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'CPT', '83036', 'Hemoglobin A1c', 0.95, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(MINUTE, -45, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'CPT', '80053', 'Comprehensive metabolic panel', 0.92, 'Verified', @StaffUserId, DATEADD(DAY, -2, GETUTCDATE()), NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 0),
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'CPT', '36415', 'Collection of venous blood by venipuncture', 0.98, 'Verified', @StaffUserId, DATEADD(DAY, -2, GETUTCDATE()), NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 0)

PRINT 'Inserted 4 CPT codes for Jane Doe'

-- Patient 2: John Smith - CPT Codes
INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'CPT', '99214', 'Office or other outpatient visit for the evaluation and management of an established patient, moderate to high complexity', 0.82, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -5, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'CPT', '94010', 'Spirometry, including graphic record, total and timed vital capacity, expiratory flow rate measurement', 0.91, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -4, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'CPT', '93000', 'Electrocardiogram, routine ECG with at least 12 leads', 0.94, 'Pending', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(HOUR, -3, GETUTCDATE()), GETUTCDATE(), 0),
    (NEWID(), @TestPatientId2, @TestDocumentId2, NULL, 'CPT', '71046', 'Radiologic examination, chest; 2 views', 0.89, 'Verified', @StaffUserId, DATEADD(DAY, -1, GETUTCDATE()), NULL, 0, NULL, NULL, NULL, NULL, NULL, DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), 0)

PRINT 'Inserted 4 CPT codes for John Smith'

-- ============================================================================
-- Overridden code example (VerificationStatus = 'Overridden')
-- ============================================================================

INSERT INTO [clinical].[MedicalCodes] 
    ([Id], [PatientId], [DocumentId], [ExtractedDataId], [CodeType], [Code], [Description], [ConfidenceScore], [VerificationStatus], [VerifiedByUserId], [VerifiedAt], [RejectionReason], [IsOverridden], [OriginalAiCode], [OriginalAiDescription], [OriginalAiConfidence], [OverrideReason], [OverrideNotes], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES
    (NEWID(), @TestPatientId, @TestDocumentId, NULL, 'ICD10', 'E11.9', 'Type 2 diabetes mellitus without complications', 0.95, 'Overridden', @StaffUserId, DATEADD(DAY, -15, GETUTCDATE()), NULL, 1, 'E11.65', 'Type 2 diabetes mellitus with hyperglycemia', 0.88, 'Patient no longer has hyperglycemia per latest labs', 'A1c now at 6.1%, reclassified', DATEADD(DAY, -20, GETUTCDATE()), DATEADD(DAY, -15, GETUTCDATE()), 0)

PRINT 'Inserted 1 overridden ICD-10 code for Jane Doe'

-- ============================================================================
-- Summary
-- ============================================================================

SELECT 
    'MedicalCodes Summary' AS [Report],
    COUNT(*) AS [TotalCodes],
    COUNT(CASE WHEN CodeType = 'ICD10' THEN 1 END) AS [ICD10_Codes],
    COUNT(CASE WHEN CodeType = 'CPT' THEN 1 END) AS [CPT_Codes],
    COUNT(CASE WHEN VerificationStatus = 'Pending' THEN 1 END) AS [Pending],
    COUNT(CASE WHEN VerificationStatus = 'Verified' THEN 1 END) AS [Verified],
    COUNT(CASE WHEN VerificationStatus = 'Rejected' THEN 1 END) AS [Rejected],
    COUNT(CASE WHEN VerificationStatus = 'Overridden' THEN 1 END) AS [Overridden]
FROM [clinical].[MedicalCodes]
WHERE [PatientId] IN (@TestPatientId, @TestPatientId2)

-- Agreement rate calculation
DECLARE @TotalVerified INT = (SELECT COUNT(*) FROM [clinical].[MedicalCodes] WHERE VerificationStatus IN ('Verified', 'Rejected', 'Overridden') AND VerifiedAt >= DATEADD(DAY, -30, GETUTCDATE()))
DECLARE @Accepted INT = (SELECT COUNT(*) FROM [clinical].[MedicalCodes] WHERE VerificationStatus = 'Verified' AND VerifiedAt >= DATEADD(DAY, -30, GETUTCDATE()))
DECLARE @AgreementRate DECIMAL(5,2) = CASE WHEN @TotalVerified > 0 THEN (@Accepted * 100.0 / @TotalVerified) ELSE 100.00 END

SELECT 
    '30-Day Agreement Rate' AS [Metric],
    @AgreementRate AS [Rate],
    @Accepted AS [Accepted],
    @TotalVerified AS [Total_Processed]

PRINT ''
PRINT '============================================'
PRINT 'Medical Code seed data complete!'
PRINT 'Test Patient 1 ID: ' + CAST(@TestPatientId AS VARCHAR(36)) + ' (Jane Doe)'
PRINT 'Test Patient 2 ID: ' + CAST(@TestPatientId2 AS VARCHAR(36)) + ' (John Smith)'
PRINT 'Staff User ID: ' + CAST(@StaffUserId AS VARCHAR(36)) + ' (Dr. Martinez)'
PRINT '============================================'
GO
