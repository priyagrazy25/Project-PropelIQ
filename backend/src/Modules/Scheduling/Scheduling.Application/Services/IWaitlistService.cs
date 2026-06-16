using SharedKernel.Domain;

namespace Scheduling.Application.Services;

public interface IWaitlistService
{
    /// <summary>
    /// Enrolls a patient on the waitlist for a specific provider with a preferred date range.
    /// </summary>
    Task<Result<WaitlistEntryResult>> EnrollAsync(Guid patientId, Guid providerId, DateTime preferredDateStart, DateTime preferredDateEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active waitlist entries for a patient.
    /// </summary>
    Task<IReadOnlyList<WaitlistEntryResult>> GetPatientEntriesAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a patient from a specific waitlist entry.
    /// </summary>
    Task<Result<bool>> RemoveAsync(Guid patientId, Guid waitlistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a released slot against the waitlist and notifies matched patients.
    /// </summary>
    Task ProcessSlotReleaseForWaitlistAsync(Guid releasedSlotId, Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires stale waitlist entries whose preferred date range has passed.
    /// </summary>
    Task ExpireStaleEntriesAsync(CancellationToken cancellationToken = default);
}

public sealed record WaitlistEntryResult(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    string Specialty,
    DateTime PreferredDateStart,
    DateTime PreferredDateEnd,
    string Status,
    int Position,
    DateTime CreatedAt,
    DateTime? NotifiedAt);
