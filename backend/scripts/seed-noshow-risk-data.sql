-- ============================================================================
-- Seed Script: No-Show Risk Assessment Sample Data
-- Purpose: Populate Appointments and NoShowRiskScores for testing risk dashboard
-- Usage: Run after seed-appointment-slots.sql to create realistic test data
-- ============================================================================

USE [UnifiedPatientAccess]
GO

SET QUOTED_IDENTIFIER ON
GO

PRINT '============================================'
PRINT 'Seeding No-Show Risk Assessment Data'
PRINT '============================================'

-- ============================================================================
-- Get or create test patients
-- ============================================================================

DECLARE @PatientId1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') ORDER BY Email)
DECLARE @PatientId2 UNIQUEIDENTIFIER
DECLARE @PatientId3 UNIQUEIDENTIFIER
DECLARE @PatientId4 UNIQUEIDENTIFIER
DECLARE @PatientId5 UNIQUEIDENTIFIER
DECLARE @PatientId6 UNIQUEIDENTIFIER
DECLARE @PatientId7 UNIQUEIDENTIFIER
DECLARE @PatientId8 UNIQUEIDENTIFIER

IF @PatientId1 IS NULL
BEGIN
    PRINT 'No patients found - creating sample patients'
    
    SET @PatientId1 = NEWID()
    SET @PatientId2 = NEWID()
    SET @PatientId3 = NEWID()
    SET @PatientId4 = NEWID()
    SET @PatientId5 = NEWID()
    SET @PatientId6 = NEWID()
    SET @PatientId7 = NEWID()
    SET @PatientId8 = NEWID()
    
    -- Note: Password hash is for 'Test@123' using Argon2id
    DECLARE @PasswordHash NVARCHAR(200) = '$argon2id$v=19$m=65536,t=3,p=1$dGVzdHNhbHQ$testhashdevelopment'
    
    INSERT INTO [identity].[Users] ([Id], [Email], [PasswordHash], [FullName], [DateOfBirth], [ContactNumber], [Address], [Role], [Status], [CreatedAt], [UpdatedAt], [IsDeleted])
    VALUES
        (@PatientId1, 'sarah.johnson@example.com', @PasswordHash, 'Sarah Johnson', '1985-03-15', '5551234567', '123 Oak Street, Springfield, IL', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId2, 'michael.brown@example.com', @PasswordHash, 'Michael Brown', '1978-07-22', '5552345678', '456 Maple Ave, Chicago, IL', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId3, 'jennifer.martinez@example.com', @PasswordHash, 'Jennifer Martinez', '1990-11-08', '5553456789', '789 Pine Blvd, Dallas, TX', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId4, 'robert.williams@example.com', @PasswordHash, 'Robert Williams', '1965-02-28', '5554567890', '321 Elm Court, Phoenix, AZ', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId5, 'emily.davis@example.com', @PasswordHash, 'Emily Davis', '1992-09-12', '5555678901', '654 Cedar Lane, Seattle, WA', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId6, 'david.lee@example.com', @PasswordHash, 'David Lee', '1988-04-05', '5556789012', '987 Birch Road, Denver, CO', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId7, 'amanda.taylor@example.com', @PasswordHash, 'Amanda Taylor', '1975-12-20', '5557890123', '147 Willow Way, Miami, FL', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0),
        (@PatientId8, 'christopher.moore@example.com', @PasswordHash, 'Christopher Moore', '1982-06-30', '5558901234', '258 Spruce Circle, Boston, MA', 'Patient', 'Active', GETUTCDATE(), GETUTCDATE(), 0)
    
    PRINT 'Created 8 sample patients'
END
ELSE
BEGIN
    -- Get existing patient IDs
    SELECT TOP 8 @PatientId1 = Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') ORDER BY Email
    SET @PatientId2 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id != @PatientId1 ORDER BY Email), NEWID())
    SET @PatientId3 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id NOT IN (@PatientId1, @PatientId2) ORDER BY Email), NEWID())
    SET @PatientId4 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id NOT IN (@PatientId1, @PatientId2, @PatientId3) ORDER BY Email), NEWID())
    SET @PatientId5 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id NOT IN (@PatientId1, @PatientId2, @PatientId3, @PatientId4) ORDER BY Email), NEWID())
    SET @PatientId6 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id NOT IN (@PatientId1, @PatientId2, @PatientId3, @PatientId4, @PatientId5) ORDER BY Email), NEWID())
    SET @PatientId7 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id NOT IN (@PatientId1, @PatientId2, @PatientId3, @PatientId4, @PatientId5, @PatientId6) ORDER BY Email), NEWID())
    SET @PatientId8 = ISNULL((SELECT TOP 1 Id FROM [identity].[Users] WHERE Role IN ('0', 'Patient') AND Id NOT IN (@PatientId1, @PatientId2, @PatientId3, @PatientId4, @PatientId5, @PatientId6, @PatientId7) ORDER BY Email), NEWID())
END

-- ============================================================================
-- Get provider IDs
-- ============================================================================

DECLARE @ProviderId1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 ORDER BY Name)
DECLARE @ProviderId2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 AND Id != @ProviderId1 ORDER BY Name)
DECLARE @ProviderId3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 AND Id NOT IN (@ProviderId1, @ProviderId2) ORDER BY Name)

IF @ProviderId1 IS NULL
BEGIN
    PRINT 'ERROR: No providers found. Run seed-appointment-slots.sql first.'
    RETURN
END

PRINT 'Using providers:'
PRINT 'Provider 1: ' + CAST(@ProviderId1 AS VARCHAR(36))
PRINT 'Provider 2: ' + CAST(ISNULL(@ProviderId2, @ProviderId1) AS VARCHAR(36))
PRINT 'Provider 3: ' + CAST(ISNULL(@ProviderId3, @ProviderId1) AS VARCHAR(36))

-- Use first provider if others don't exist
SET @ProviderId2 = ISNULL(@ProviderId2, @ProviderId1)
SET @ProviderId3 = ISNULL(@ProviderId3, @ProviderId1)

-- ============================================================================
-- Clear existing test appointments (keep production data)
-- ============================================================================

DELETE FROM [scheduling].[NoShowRiskScores] WHERE AppointmentId IN (
    SELECT Id FROM [scheduling].[Appointments] WHERE Notes = 'Seeded for risk dashboard testing'
)
DELETE FROM [scheduling].[Appointments] WHERE Notes = 'Seeded for risk dashboard testing'
PRINT 'Cleared existing seeded appointments'

-- ============================================================================
-- Create appointments with various risk levels for next 7 days
-- ============================================================================

DECLARE @Now DATETIME2 = GETUTCDATE()
DECLARE @Today DATE = CAST(@Now AS DATE)

-- Appointment 1: High risk (85) - 2 prior no-shows, last-minute booking
DECLARE @AptId1 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime1 DATETIME2 = DATEADD(HOUR, 2, @Now)

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId1, @PatientId1, @ProviderId1, NULL, @AptTime1, 30, 'InPerson', 'Scheduled', 'Follow-up consultation', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId1, @PatientId1, 85, '2 prior no-shows; Last-minute booking; No insurance on file', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 2: High risk (78) - Distance > 30 miles, first-time patient
DECLARE @AptId2 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime2 DATETIME2 = DATEADD(HOUR, 4, @Now)

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId2, @PatientId2, @ProviderId2, NULL, @AptTime2, 30, 'InPerson', 'Scheduled', 'Initial consultation', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId2, @PatientId2, 78, 'Distance > 30 miles; First-time patient; Monday AM slot', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 3: Medium risk (55) - 1 prior no-show
DECLARE @AptId3 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime3 DATETIME2 = DATEADD(HOUR, 6, @Now)

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId3, @PatientId3, @ProviderId1, NULL, @AptTime3, 30, 'InPerson', 'Scheduled', 'Routine checkup', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId3, @PatientId3, 55, '1 prior no-show; Weather advisory', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 4: Medium risk (48) - Unconfirmed
DECLARE @AptId4 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime4 DATETIME2 = DATEADD(HOUR, 8, @Now)

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId4, @PatientId4, @ProviderId3, NULL, @AptTime4, 30, 'InPerson', 'Scheduled', 'Lab review', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId4, @PatientId4, 48, 'Unconfirmed appointment', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 5: Medium risk (35) - Weekend slot
DECLARE @AptId5 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime5 DATETIME2 = DATEADD(DAY, 1, DATEADD(HOUR, 10, CAST(@Today AS DATETIME2)))

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId5, @PatientId5, @ProviderId2, NULL, @AptTime5, 30, 'InPerson', 'Scheduled', 'Physical exam', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId5, @PatientId5, 35, 'Weekend slot', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 6: Low risk (22) - Regular patient
DECLARE @AptId6 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime6 DATETIME2 = DATEADD(DAY, 1, DATEADD(HOUR, 14, CAST(@Today AS DATETIME2)))

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId6, @PatientId6, @ProviderId1, NULL, @AptTime6, 30, 'InPerson', 'Scheduled', 'Follow-up', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId6, @PatientId6, 22, '', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 7: Low risk (15) - Established patient
DECLARE @AptId7 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime7 DATETIME2 = DATEADD(DAY, 2, DATEADD(HOUR, 9, CAST(@Today AS DATETIME2)))

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId7, @PatientId7, @ProviderId3, NULL, @AptTime7, 30, 'InPerson', 'Scheduled', 'Annual wellness visit', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId7, @PatientId7, 15, '', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- Appointment 8: Low risk (8) - Confirmed, regular patient
DECLARE @AptId8 UNIQUEIDENTIFIER = NEWID()
DECLARE @AptTime8 DATETIME2 = DATEADD(DAY, 3, DATEADD(HOUR, 11, CAST(@Today AS DATETIME2)))

INSERT INTO [scheduling].[Appointments] 
    ([Id], [PatientId], [ProviderId], [SlotId], [AppointmentDateTime], [DurationMinutes], [Type], [Status], [Reason], [Notes], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (@AptId8, @PatientId8, @ProviderId2, NULL, @AptTime8, 30, 'InPerson', 'Confirmed', 'Chronic care management', 'Seeded for risk dashboard testing', 0, GETUTCDATE(), GETUTCDATE())

INSERT INTO [scheduling].[NoShowRiskScores] 
    ([Id], [AppointmentId], [PatientId], [Score], [RiskFactors], [CalculatedAt], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES 
    (NEWID(), @AptId8, @PatientId8, 8, '', GETUTCDATE(), 0, GETUTCDATE(), GETUTCDATE())

-- ============================================================================
-- Summary Report
-- ============================================================================

PRINT ''
PRINT '============================================'
PRINT 'No-Show Risk Seed Data Summary'
PRINT '============================================'

SELECT 
    'Risk Distribution' AS [Report],
    CASE 
        WHEN Score > 70 THEN 'High'
        WHEN Score > 30 THEN 'Medium'
        ELSE 'Low'
    END AS RiskLevel,
    COUNT(*) AS Count
FROM [scheduling].[NoShowRiskScores]
WHERE AppointmentId IN (
    SELECT Id FROM [scheduling].[Appointments] WHERE Notes = 'Seeded for risk dashboard testing'
)
GROUP BY 
    CASE 
        WHEN Score > 70 THEN 'High'
        WHEN Score > 30 THEN 'Medium'
        ELSE 'Low'
    END

SELECT 
    'Seeded Appointments' AS [Report],
    a.Id AS AppointmentId,
    u.FullName AS PatientName,
    p.Name AS ProviderName,
    a.AppointmentDateTime,
    r.Score AS RiskScore,
    CASE 
        WHEN r.Score > 70 THEN 'High'
        WHEN r.Score > 30 THEN 'Medium'
        ELSE 'Low'
    END AS RiskLevel,
    r.RiskFactors
FROM [scheduling].[Appointments] a
INNER JOIN [scheduling].[NoShowRiskScores] r ON a.Id = r.AppointmentId
LEFT JOIN [identity].[Users] u ON a.PatientId = u.Id
LEFT JOIN [scheduling].[Providers] p ON a.ProviderId = p.Id
WHERE a.Notes = 'Seeded for risk dashboard testing'
ORDER BY r.Score DESC

PRINT ''
PRINT 'Seed data complete!'
PRINT '- 8 appointments created'
PRINT '- 2 high-risk (>70), 3 medium-risk (31-70), 3 low-risk (<=30)'
PRINT '============================================'
GO
