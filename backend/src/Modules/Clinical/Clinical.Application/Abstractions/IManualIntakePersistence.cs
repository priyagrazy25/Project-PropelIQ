using Clinical.Application.DTOs;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Cross-module abstraction for manual intake record persistence.
/// Implemented in the Host project which has access to the Scheduling module.
/// </summary>
public interface IManualIntakePersistence
{
    Task<ManualIntakeResponse?> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<ManualIntakeResponse> UpsertDraftAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default);

    Task<ManualIntakeResponse> SubmitAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default);

    Task<ManualIntakeResponse?> UpdateFieldAsync(
        Guid appointmentId,
        string fieldName,
        string value,
        CancellationToken cancellationToken = default);
}
