-- Seed scheduled appointments for No-Show Risk Assessment testing
-- Run this script to create future scheduled appointments

DECLARE @today DATE = CAST(GETDATE() AS DATE);
DECLARE @tomorrow DATE = DATEADD(DAY, 1, @today);
DECLARE @nextWeek DATE = DATEADD(DAY, 7, @today);

-- Provider IDs (from existing data)
DECLARE @provider1 UNIQUEIDENTIFIER = '24513CF0-395E-482B-9E7F-D227B4F77671'; -- Dr. Lisa Thompson
DECLARE @provider2 UNIQUEIDENTIFIER = '6EC1722C-3E35-41B4-A81B-2C9038FB3B8D'; -- Dr. Sarah Johnson
DECLARE @provider3 UNIQUEIDENTIFIER = '3F7DD35E-CD53-49AF-998E-40D200FFC01F'; -- Dr. David Kim

-- Patient IDs (from existing data)
DECLARE @patient1 UNIQUEIDENTIFIER = '8dec460b-c38f-4aab-a85f-629f3ce5a097'; -- test new
DECLARE @patient2 UNIQUEIDENTIFIER = 'fe495616-a182-4620-a4f7-76e3740ad77e'; -- Test Patient
DECLARE @patient3 UNIQUEIDENTIFIER = '919722ce-5593-44c3-82bb-2d27245b57ea'; -- test pp

-- Insert scheduled appointments for tomorrow and next week
-- Status = 0 (Scheduled)
INSERT INTO scheduling.Appointments (Id, PatientId, ProviderId, AppointmentDateTime, DurationMinutes, [Type], [Status], Reason, IsDeleted, CreatedAt, UpdatedAt, RowVersion)
VALUES
    -- Tomorrow morning appointments
    (NEWID(), @patient1, @provider1, DATEADD(HOUR, 9, CAST(@tomorrow AS DATETIME2)), 30, 0, 0, 'Annual checkup', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    (NEWID(), @patient2, @provider2, DATEADD(HOUR, 10, CAST(@tomorrow AS DATETIME2)), 30, 0, 0, 'Follow-up visit', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    (NEWID(), @patient3, @provider3, DATEADD(HOUR, 11, CAST(@tomorrow AS DATETIME2)), 45, 0, 0, 'Consultation', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    
    -- Tomorrow afternoon appointments
    (NEWID(), @patient1, @provider2, DATEADD(HOUR, 14, CAST(@tomorrow AS DATETIME2)), 30, 0, 0, 'Blood pressure check', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    (NEWID(), @patient2, @provider3, DATEADD(HOUR, 15, CAST(@tomorrow AS DATETIME2)), 30, 0, 0, 'Lab results review', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    
    -- Next week appointments
    (NEWID(), @patient3, @provider1, DATEADD(HOUR, 9, CAST(@nextWeek AS DATETIME2)), 30, 0, 0, 'Physical examination', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    (NEWID(), @patient1, @provider3, DATEADD(HOUR, 10, CAST(@nextWeek AS DATETIME2)), 45, 0, 0, 'Specialist consultation', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000),
    (NEWID(), @patient2, @provider1, DATEADD(HOUR, 14, CAST(@nextWeek AS DATETIME2)), 30, 0, 0, 'Follow-up', 0, GETUTCDATE(), GETUTCDATE(), 0x00000000);

SELECT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' scheduled appointments for risk assessment testing' AS Result;

-- Verify the inserted data
SELECT 
    a.Id,
    a.PatientId,
    a.ProviderId,
    p.Name AS ProviderName,
    a.AppointmentDateTime,
    a.Status,
    a.Reason
FROM scheduling.Appointments a
JOIN scheduling.Providers p ON a.ProviderId = p.Id
WHERE a.Status = 0 -- Scheduled
  AND a.AppointmentDateTime >= GETDATE()
ORDER BY a.AppointmentDateTime;
