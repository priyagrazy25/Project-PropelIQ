using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Identity.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public RefreshTokenCommandHandler(
        IIdentityDbContext dbContext,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<RefreshTokenResult>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result<RefreshTokenResult>.Failure("INVALID_TOKEN");
        }

        // Find the refresh token (including revoked ones for reuse detection)
        var existingToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == command.Token && !rt.IsDeleted, cancellationToken);

        if (existingToken is null)
        {
            return Result<RefreshTokenResult>.Failure("INVALID_TOKEN");
        }

        // Token reuse detection: if already revoked, revoke entire family
        if (existingToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(existingToken.UserId, cancellationToken);
            return Result<RefreshTokenResult>.Failure("TOKEN_REUSE_DETECTED");
        }

        // Check expiration
        if (existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            existingToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefreshTokenResult>.Failure("TOKEN_EXPIRED");
        }

        // Check user status
        if (existingToken.User.Status != UserStatus.Active)
        {
            existingToken.IsRevoked = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefreshTokenResult>.Failure("ACCOUNT_INACTIVE");
        }

        // Rotate: revoke old, create new
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

        return Result<RefreshTokenResult>.Success(
            new RefreshTokenResult(accessToken, newTokenValue));
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
