-- ============================================================================
-- Seed Script: 360-Degree Patient View Test Data
-- Purpose: Populate ExtractedData table with sample clinical data for testing
-- Usage: Run after database migrations are applied
-- ============================================================================

USE [UnifiedPatientAccess]
GO

-- ============================================================================
-- Prerequisites: Ensure a test patient and document exist
-- ============================================================================

-- Create test user if not exists
DECLARE @TestUserId UNIQUEIDENTIFIER = 'A1B2C3D4-E5F6-4A5B-8C7D-9E0F1A2B3C4D'
DECLARE @TestPatientId UNIQUEIDENTIFIER = 'B2C3D4E5-F6A7-5B6C-9D8E-0F1A2B3C4D5E'
DECLARE @TestDocumentId UNIQUEIDENTIFIER = 'C3D4E5F6-A7B8-6C7D-0E9F-1A2B3C4D5E6F'

-- Insert test user (if not exists)
IF NOT EXISTS (SELECT 1 FROM [identity].[Users] WHERE [Id] = @TestUserId)
BEGIN
    INSERT INTO [identity].[Users] 
        ([Id], [Email], [PasswordHash], [FullName], [DateOfBirth], [ContactNumber], [Address], [Role], [Status], [CreatedAt], [UpdatedAt], [IsDeleted], [RowVersion])
    VALUES 
        (@TestUserId, 'jane.doe@test.com', '$2a$11$TestHashForDevelopmentOnly', 'Jane Marie Doe', '1985-03-15', '(555) 123-4567', '123 Main St, Springfield, IL 62701', 0, 1, GETUTCDATE(), GETUTCDATE(), 0, 0x00000001)
    
    PRINT 'Created test user: jane.doe@test.com'
END

-- Insert test patient (if not exists)
IF NOT EXISTS (SELECT 1 FROM [identity].[Patients] WHERE [Id] = @TestPatientId)
BEGIN
    INSERT INTO [identity].[Patients]
        ([Id], [UserId], [InsuranceProvider], [InsurancePolicyNumber], [EmergencyContactName], [EmergencyContactPhone], [IntakeCompleted], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@TestPatientId, @TestUserId, 'Blue Cross Blue Shield', 'BCBS-12345678', 'John Doe', '(555) 987-6543', 1, GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created test patient'
END

-- Insert test clinical document (if not exists)
IF NOT EXISTS (SELECT 1 FROM [clinical].[ClinicalDocuments] WHERE [Id] = @TestDocumentId)
BEGIN
    INSERT INTO [clinical].[ClinicalDocuments]
        ([Id], [PatientId], [FileName], [EncryptedFilePath], [ContentType], [FileSizeBytes], [ProcessingStatus], [ProcessedAt], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@TestDocumentId, @TestPatientId, 'Lab_Report_2026-04-20.pdf', '/encrypted/docs/test-doc.enc', 'application/pdf', 1024000, 4, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created test clinical document'
END

-- ============================================================================
-- Seed ExtractedData for 360-Degree Patient View
-- ============================================================================

-- Clear existing test data for this patient
DELETE FROM [clinical].[ExtractedData] WHERE [PatientId] = @TestPatientId
PRINT 'Cleared existing extracted data for test patient'

-- VitalSign (Category = 5)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 5, 'Blood Pressure', '120/80 mmHg', 0.95, 1, 'BP: 120/80', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 5, 'Heart Rate', '72 bpm', 0.97, 1, 'HR: 72', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 5, 'Temperature', '98.6°F', 0.96, 1, 'Temp: 98.6F', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 5, 'Blood Type', 'O+', 0.78, 2, 'Blood type O positive', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 5, 'Weight', '145 lbs', 0.94, 1, 'Weight: 145 lbs', GETUTCDATE(), GETUTCDATE(), 0)

PRINT 'Inserted VitalSign data (5 records)'

-- Procedure/Medical History (Category = 3)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 3, 'Appendectomy', '2018', 0.92, 3, 'Appendectomy performed 2018', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 3, 'Tonsillectomy', '2005', 0.88, 3, 'Tonsillectomy childhood', GETUTCDATE(), GETUTCDATE(), 0)

-- FamilyHistory (Category = 7)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 7, 'Diabetes', 'Mother - Type 2', 0.85, 4, 'Family hx: mother diabetes', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 7, 'Hypertension', 'Father', 0.83, 4, 'Father HTN', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 7, 'Heart Disease', 'Grandfather (paternal)', 0.75, 4, 'Paternal grandfather CAD', GETUTCDATE(), GETUTCDATE(), 0)

PRINT 'Inserted Medical History data (5 records)'

-- Medication (Category = 1)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 1, 'Lisinopril', '10mg daily', 0.94, 2, 'Lisinopril 10mg PO daily', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 1, 'Metformin', '500mg twice daily', 0.91, 2, 'Metformin 500mg BID', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 1, 'Aspirin', '81mg daily', 0.89, 2, 'ASA 81mg daily', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 1, 'Atorvastatin', '20mg at bedtime', 0.93, 2, 'Lipitor 20mg qhs', GETUTCDATE(), GETUTCDATE(), 0)

PRINT 'Inserted Medication data (4 records)'

-- Allergy (Category = 2)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 2, 'Penicillin', 'Severe - Anaphylaxis', 0.96, 1, 'ALLERGIES: PCN - anaphylaxis', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 2, 'Shellfish', 'Moderate - Hives', 0.94, 1, 'Shellfish allergy - hives', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 2, 'Sulfa drugs', 'Mild - Rash', 0.87, 1, 'Sulfa - rash', GETUTCDATE(), GETUTCDATE(), 0)

PRINT 'Inserted Allergy data (3 records)'

-- LabResult (Category = 4)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'Hemoglobin A1c', '6.2%', 0.93, 5, 'HbA1c: 6.2%', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'Total Cholesterol', '195 mg/dL', 0.91, 5, 'Total chol: 195', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'LDL Cholesterol', '120 mg/dL', 0.90, 5, 'LDL: 120', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'HDL Cholesterol', '55 mg/dL', 0.92, 5, 'HDL: 55', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'Triglycerides', '140 mg/dL', 0.89, 5, 'TG: 140', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'Fasting Glucose', '105 mg/dL', 0.94, 5, 'FBG: 105', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'Creatinine', '0.9 mg/dL', 0.95, 6, 'Cr: 0.9', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 4, 'eGFR', '>90 mL/min', 0.88, 6, 'eGFR >90', GETUTCDATE(), GETUTCDATE(), 0)

PRINT 'Inserted LabResult data (8 records)'

-- Diagnosis (Category = 0)
INSERT INTO [clinical].[ExtractedData] ([Id], [DocumentId], [PatientId], [Category], [Key], [Value], [ConfidenceScore], [SourcePage], [SourceText], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES 
    (NEWID(), @TestDocumentId, @TestPatientId, 0, 'Type 2 Diabetes Mellitus', 'E11.9 - Without complications', 0.95, 7, 'DX: Type 2 DM E11.9', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 0, 'Essential Hypertension', 'I10 - Primary', 0.93, 7, 'HTN I10', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 0, 'Hyperlipidemia', 'E78.5 - Mixed', 0.91, 7, 'Hyperlipidemia E78.5', GETUTCDATE(), GETUTCDATE(), 0),
    (NEWID(), @TestDocumentId, @TestPatientId, 0, 'Obesity', 'E66.9 - BMI 32', 0.87, 7, 'Obesity BMI 32', GETUTCDATE(), GETUTCDATE(), 0)

PRINT 'Inserted Diagnosis data (4 records)'

-- ============================================================================
-- Summary
-- ============================================================================
SELECT 
    'ExtractedData Summary' AS [Report],
    COUNT(*) AS [TotalRecords],
    COUNT(CASE WHEN Category = 5 THEN 1 END) AS [Vitals],
    COUNT(CASE WHEN Category IN (3, 7) THEN 1 END) AS [History],
    COUNT(CASE WHEN Category = 1 THEN 1 END) AS [Medications],
    COUNT(CASE WHEN Category = 2 THEN 1 END) AS [Allergies],
    COUNT(CASE WHEN Category = 4 THEN 1 END) AS [Labs],
    COUNT(CASE WHEN Category = 0 THEN 1 END) AS [Diagnoses]
FROM [clinical].[ExtractedData]
WHERE [PatientId] = @TestPatientId

PRINT ''
PRINT '============================================'
PRINT 'Seed data complete!'
PRINT 'Test Patient ID: ' + CAST(@TestPatientId AS VARCHAR(36))
PRINT 'Test User Email: jane.doe@test.com'
PRINT 'Test Password: (set via registration or update hash)'
PRINT '============================================'
GO
