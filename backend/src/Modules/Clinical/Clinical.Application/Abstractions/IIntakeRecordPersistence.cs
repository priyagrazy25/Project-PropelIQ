namespace Clinical.Application.Abstractions;

/// <summary>
/// Cross-module abstraction for persisting intake records to the Scheduling module.
/// Implemented in the Host project which has access to both modules.
/// </summary>
public interface IIntakeRecordPersistence
{
    Task PersistAsync(
        Guid patientId,
        Guid? appointmentId,
        string? chiefComplaint,
        string? currentMedications,
        string? allergies,
        string? medicalHistory,
        CancellationToken cancellationToken = default);
}
