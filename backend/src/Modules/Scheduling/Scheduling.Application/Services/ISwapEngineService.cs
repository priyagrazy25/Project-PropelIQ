namespace Scheduling.Application.Services;

public sealed record SwapExecutionResult(
    Guid SwapId,
    Guid PatientId,
    Guid ProviderId,
    string ProviderName,
    Guid OriginalSlotId,
    Guid NewSlotId,
    DateTime NewSlotStart,
    DateTime NewSlotEnd);

public interface ISwapEngineService
{
    /// <summary>
    /// Processes swap queue for a released slot. Returns the executed swap or null if no eligible swap found.
    /// </summary>
    Task<SwapExecutionResult?> ProcessSlotReleaseAsync(Guid releasedSlotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires all pending swap preferences for a cancelled appointment.
    /// </summary>
    Task ExpireSwapsForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
