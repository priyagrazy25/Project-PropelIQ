using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Identity.Application.Commands.Login;

public sealed class LoginCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public LoginCommandHandler(
        IIdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<LoginResult>.Failure("INVALID_CREDENTIALS");
        }

        var email = command.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return Result<LoginResult>.Failure("INVALID_CREDENTIALS");
        }

        if (user.Status != UserStatus.Active)
        {
            return Result<LoginResult>.Failure("ACCOUNT_INACTIVE");
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result<LoginResult>.Failure("INVALID_CREDENTIALS");
        }

        var roleName = user.Role.ToString();

        // Lookup PatientId for patient users (required for /me endpoints)
        Guid? patientId = null;
        if (user.Role == UserRole.Patient)
        {
            var patient = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            patientId = patient?.Id;
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, roleName, user.FullName, patientId);

        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            DeviceId = command.DeviceId,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(new LoginResult(
            accessToken, refreshTokenValue, user.Id, roleName, user.FullName));
    }
}
