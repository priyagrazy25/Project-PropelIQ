-- ============================================================================
-- Seed Script: Appointment Slots for Provider Search
-- Purpose: Populate AppointmentSlots table with available slots for testing
-- Usage: Run after migrations are applied to ensure provider search works
-- ============================================================================

USE [UnifiedPatientAccess]
GO

SET QUOTED_IDENTIFIER ON
GO

-- ============================================================================
-- Clear existing available slots (keep booked ones)
-- ============================================================================

DELETE FROM [scheduling].[AppointmentSlots] WHERE Status = 'Available'
PRINT 'Cleared existing available slots'

-- ============================================================================
-- Get provider IDs
-- ============================================================================

DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE)
DECLARE @ProviderId1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 ORDER BY Name)
DECLARE @ProviderId2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 AND Id != @ProviderId1 ORDER BY Name)
DECLARE @ProviderId3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 AND Id NOT IN (@ProviderId1, @ProviderId2) ORDER BY Name)
DECLARE @ProviderId4 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 AND Id NOT IN (@ProviderId1, @ProviderId2, @ProviderId3) ORDER BY Name)
DECLARE @ProviderId5 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [scheduling].[Providers] WHERE IsActive = 1 AND Id NOT IN (@ProviderId1, @ProviderId2, @ProviderId3, @ProviderId4) ORDER BY Name)

IF @ProviderId1 IS NULL
BEGIN
    PRINT 'No providers found - creating sample providers'
    
    SET @ProviderId1 = NEWID()
    SET @ProviderId2 = NEWID()
    SET @ProviderId3 = NEWID()
    SET @ProviderId4 = NEWID()
    SET @ProviderId5 = NEWID()
    
    INSERT INTO [scheduling].[Providers] ([Id], [Name], [Specialty], [Location], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
    VALUES
        (@ProviderId1, 'Dr. Sarah Johnson', 'Family Medicine', 'Main Clinic - 100 Medical Center Dr', 1, 0, GETUTCDATE(), GETUTCDATE()),
        (@ProviderId2, 'Dr. Michael Chen', 'Internal Medicine', 'West Wing - 200 Health Ave', 1, 0, GETUTCDATE(), GETUTCDATE()),
        (@ProviderId3, 'Dr. Emily Rodriguez', 'Cardiology', 'Heart Center - 300 Cardiac Way', 1, 0, GETUTCDATE(), GETUTCDATE()),
        (@ProviderId4, 'Dr. James Williams', 'Pediatrics', 'Children''s Building - 150 Kids Lane', 1, 0, GETUTCDATE(), GETUTCDATE()),
        (@ProviderId5, 'Dr. Lisa Thompson', 'Dermatology', 'Skin Care Clinic - 400 Derma St', 1, 0, GETUTCDATE(), GETUTCDATE())
    
    PRINT 'Created 5 sample providers'
END

PRINT 'Provider 1: ' + CAST(@ProviderId1 AS VARCHAR(36))
PRINT 'Provider 2: ' + CAST(@ProviderId2 AS VARCHAR(36))
PRINT 'Provider 3: ' + CAST(@ProviderId3 AS VARCHAR(36))
PRINT 'Provider 4: ' + CAST(@ProviderId4 AS VARCHAR(36))
PRINT 'Provider 5: ' + CAST(@ProviderId5 AS VARCHAR(36))

-- ============================================================================
-- Create slots for today and next 7 days
-- Business hours: 8 AM - 5 PM, 30-minute appointments
-- ============================================================================

DECLARE @DayOffset INT = 0
DECLARE @SlotDuration INT = 30  -- minutes
DECLARE @StartHour INT = 8      -- 8 AM
DECLARE @EndHour INT = 17       -- 5 PM

WHILE @DayOffset <= 7
BEGIN
    DECLARE @SlotDate DATE = DATEADD(DAY, @DayOffset, @Today)
    DECLARE @Hour INT = @StartHour
    
    -- Skip weekends (Saturday = 7, Sunday = 1 in SQL Server with SET DATEFIRST 7)
    IF DATEPART(WEEKDAY, @SlotDate) NOT IN (1, 7)
    BEGIN
        WHILE @Hour < @EndHour
        BEGIN
            DECLARE @SlotTime DATETIME2 = DATEADD(HOUR, @Hour, CAST(@SlotDate AS DATETIME2))
            DECLARE @SlotTime30 DATETIME2 = DATEADD(MINUTE, 30, @SlotTime)
            
            -- Provider 1: Full schedule
            INSERT INTO [scheduling].[AppointmentSlots] ([Id], [ProviderId], [StartTime], [DurationMinutes], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @ProviderId1, @SlotTime, @SlotDuration, 'Available', 0, GETUTCDATE(), GETUTCDATE())
            
            INSERT INTO [scheduling].[AppointmentSlots] ([Id], [ProviderId], [StartTime], [DurationMinutes], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @ProviderId1, @SlotTime30, @SlotDuration, 'Available', 0, GETUTCDATE(), GETUTCDATE())
            
            -- Provider 2: Morning only (8 AM - 12 PM)
            IF @Hour < 12
            BEGIN
                INSERT INTO [scheduling].[AppointmentSlots] ([Id], [ProviderId], [StartTime], [DurationMinutes], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (NEWID(), @ProviderId2, @SlotTime, @SlotDuration, 'Available', 0, GETUTCDATE(), GETUTCDATE())
            END
            
            -- Provider 3: Afternoon only (1 PM - 5 PM)
            IF @Hour >= 13
            BEGIN
                INSERT INTO [scheduling].[AppointmentSlots] ([Id], [ProviderId], [StartTime], [DurationMinutes], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (NEWID(), @ProviderId3, @SlotTime, @SlotDuration, 'Available', 0, GETUTCDATE(), GETUTCDATE())
            END
            
            -- Provider 4: Every other hour
            IF @Hour % 2 = 0
            BEGIN
                INSERT INTO [scheduling].[AppointmentSlots] ([Id], [ProviderId], [StartTime], [DurationMinutes], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (NEWID(), @ProviderId4, @SlotTime, @SlotDuration, 'Available', 0, GETUTCDATE(), GETUTCDATE())
            END
            
            -- Provider 5: Limited availability (10 AM - 3 PM)
            IF @Hour >= 10 AND @Hour < 15
            BEGIN
                INSERT INTO [scheduling].[AppointmentSlots] ([Id], [ProviderId], [StartTime], [DurationMinutes], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (NEWID(), @ProviderId5, @SlotTime, @SlotDuration, 'Available', 0, GETUTCDATE(), GETUTCDATE())
            END
            
            SET @Hour = @Hour + 1
        END
    END
    
    SET @DayOffset = @DayOffset + 1
END

-- ============================================================================
-- Summary
-- ============================================================================

SELECT 
    'Appointment Slots Summary' AS [Report],
    p.Name AS ProviderName,
    COUNT(*) AS TotalSlots,
    MIN(s.StartTime) AS EarliestSlot,
    MAX(s.StartTime) AS LatestSlot
FROM [scheduling].[AppointmentSlots] s
INNER JOIN [scheduling].[Providers] p ON s.ProviderId = p.Id
WHERE s.Status = 'Available' AND s.StartTime >= @Today
GROUP BY p.Name
ORDER BY p.Name

SELECT 
    'Total Available Slots by Day' AS [Report],
    CONVERT(DATE, StartTime) AS SlotDate,
    COUNT(*) AS AvailableSlots
FROM [scheduling].[AppointmentSlots]
WHERE Status = 'Available' AND StartTime >= @Today
GROUP BY CONVERT(DATE, StartTime)
ORDER BY SlotDate

PRINT ''
PRINT '============================================'
PRINT 'Appointment slots seed data complete!'
PRINT 'Slots created for: ' + CAST(@Today AS VARCHAR(10)) + ' to ' + CAST(DATEADD(DAY, 7, @Today) AS VARCHAR(10))
PRINT '============================================'
GO
