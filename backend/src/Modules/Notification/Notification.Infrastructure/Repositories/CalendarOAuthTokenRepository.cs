using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Repositories;

public sealed class CalendarOAuthTokenRepository : ICalendarOAuthTokenRepository
{
    private readonly NotificationDbContext _dbContext;

    public CalendarOAuthTokenRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CalendarOAuthToken?> GetByPatientAndProviderAsync(
        Guid patientId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CalendarOAuthTokens
            .FirstOrDefaultAsync(
                t => t.PatientId == patientId
                     && t.Provider == provider
                     && !t.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarOAuthToken>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CalendarOAuthTokens
            .AsNoTracking()
            .Where(t => t.PatientId == patientId && !t.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(CalendarOAuthToken token, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.CalendarOAuthTokens
            .FirstOrDefaultAsync(
                t => t.PatientId == token.PatientId
                     && t.Provider == token.Provider
                     && !t.IsDeleted,
                cancellationToken);

        if (existing is not null)
        {
            existing.EncryptedAccessToken = token.EncryptedAccessToken;
            existing.EncryptedRefreshToken = token.EncryptedRefreshToken;
            existing.AccessTokenExpiresAt = token.AccessTokenExpiresAt;
        }
        else
        {
            _dbContext.CalendarOAuthTokens.Add(token);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid patientId, string provider, CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.CalendarOAuthTokens
            .FirstOrDefaultAsync(
                t => t.PatientId == patientId && t.Provider == provider && !t.IsDeleted,
                cancellationToken);

        if (token is not null)
        {
            token.IsDeleted = true;
            token.DeletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
