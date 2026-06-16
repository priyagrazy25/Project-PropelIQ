using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Identity.Application.Commands.Logout;

public sealed class LogoutCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ISessionTrackingService _sessionTracking;
    private readonly ITokenBlacklistService _tokenBlacklist;

    public LogoutCommandHandler(
        IIdentityDbContext dbContext,
        ISessionTrackingService sessionTracking,
        ITokenBlacklistService tokenBlacklist)
    {
        _dbContext = dbContext;
        _sessionTracking = sessionTracking;
        _tokenBlacklist = tokenBlacklist;
    }

    public async Task<Result<bool>> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        // Revoke all refresh tokens for this user
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == command.UserId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Blacklist the current access token (if jti provided) until its natural expiry
        if (!string.IsNullOrEmpty(command.Jti) && command.AccessTokenRemainingLifetime.HasValue)
        {
            await _tokenBlacklist.BlacklistTokenAsync(
                command.Jti,
                command.AccessTokenRemainingLifetime.Value,
                cancellationToken);
        }

        // Invalidate all cached sessions
        await _sessionTracking.InvalidateAllSessionsAsync(command.UserId, cancellationToken);

        return Result<bool>.Success(true);
    }
}
