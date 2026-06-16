using Clinical.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Data;

namespace Host.Services;

/// <summary>
/// Cross-module intake record persistence bridging Clinical and Scheduling modules.
/// Persists completed intake session data to the IntakeRecord entity in the Scheduling module.
/// </summary>
public sealed class IntakeRecordPersistenceService : IIntakeRecordPersistence
{
    private readonly SchedulingDbContext _schedulingDb;

    public IntakeRecordPersistenceService(SchedulingDbContext schedulingDb)
    {
        _schedulingDb = schedulingDb;
    }

    public async Task PersistAsync(
        Guid patientId,
        Guid? appointmentId,
        string? chiefComplaint,
        string? currentMedications,
        string? allergies,
        string? medicalHistory,
        CancellationToken cancellationToken = default)
    {
        var record = new IntakeRecord
        {
            PatientId = patientId,
            AppointmentId = appointmentId,
            ChiefComplaint = chiefComplaint,
            CurrentMedications = currentMedications,
            Allergies = allergies,
            MedicalHistory = medicalHistory,
            IsComplete = true,
            CompletedAt = DateTime.UtcNow
        };

        _schedulingDb.IntakeRecords.Add(record);
        await _schedulingDb.SaveChangesAsync(cancellationToken);
    }
}
