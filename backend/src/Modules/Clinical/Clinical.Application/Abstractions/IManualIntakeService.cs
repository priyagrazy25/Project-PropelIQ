using Clinical.Application.DTOs;
using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Manual intake service for form-based intake (AC-1 through AC-4).
/// </summary>
public interface IManualIntakeService
{
    Task<Result<ManualIntakeResponse>> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<Result<ManualIntakeResponse>> AutosaveAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ManualIntakeResponse>> SubmitAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ManualIntakeResponse>> UpdateFieldsAsync(
        Guid appointmentId,
        IntakeFieldUpdateRequest request,
        CancellationToken cancellationToken = default);
}
