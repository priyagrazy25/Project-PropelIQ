using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

public interface ICalendarOAuthTokenRepository
{
    Task<CalendarOAuthToken?> GetByPatientAndProviderAsync(
        Guid patientId,
        string provider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarOAuthToken>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(CalendarOAuthToken token, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid patientId, string provider, CancellationToken cancellationToken = default);
}
