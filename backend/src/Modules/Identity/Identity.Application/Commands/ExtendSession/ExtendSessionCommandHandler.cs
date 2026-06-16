using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Identity.Application.Commands.ExtendSession;

public sealed class ExtendSessionCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionTrackingService _sessionTracking;

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(15);

    public ExtendSessionCommandHandler(
        IIdentityDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ISessionTrackingService sessionTracking)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _sessionTracking = sessionTracking;
    }

    public async Task<Result<ExtendSessionResult>> HandleAsync(
        ExtendSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result<ExtendSessionResult>.Failure("INVALID_TOKEN");
        }

        var existingToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken && !rt.IsDeleted, cancellationToken);

        if (existingToken is null)
        {
            return Result<ExtendSessionResult>.Failure("INVALID_TOKEN");
        }

        // Token reuse detection
        if (existingToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(existingToken.UserId, cancellationToken);
            await _sessionTracking.InvalidateAllSessionsAsync(existingToken.UserId, cancellationToken);
            return Result<ExtendSessionResult>.Failure("TOKEN_REUSE_DETECTED");
        }

        if (existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            existingToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<ExtendSessionResult>.Failure("SESSION_EXPIRED");
        }

        if (existingToken.User.Status != UserStatus.Active)
        {
            existingToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<ExtendSessionResult>.Failure("ACCOUNT_INACTIVE");
        }

        // Rotate tokens
        var newTokenValue = _jwtTokenService.GenerateRefreshToken();

        existingToken.IsRevoked = true;
        existingToken.ReplacedByToken = newTokenValue;

        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            UserId = existingToken.UserId,
            Token = newTokenValue,
            DeviceId = command.DeviceId ?? existingToken.DeviceId,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);

        var user = existingToken.User;

        // Lookup PatientId for patient users (required for /me endpoints)
        Guid? patientId = null;
        if (user.Role == UserRole.Patient)
        {
            var patient = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            patientId = patient?.Id;
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.Role.ToString(), user.FullName, patientId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Extend session TTL in cache
        await _sessionTracking.TrackSessionAsync(user.Id, command.DeviceId, SessionTtl, cancellationToken);

        return Result<ExtendSessionResult>.Success(
            new ExtendSessionResult(accessToken, newTokenValue));
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
