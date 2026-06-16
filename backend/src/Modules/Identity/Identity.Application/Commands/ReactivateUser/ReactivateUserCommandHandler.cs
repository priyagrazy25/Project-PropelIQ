using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Identity.Application.Commands.ReactivateUser;

public sealed class ReactivateUserCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ILogger<ReactivateUserCommandHandler> _logger;

    public ReactivateUserCommandHandler(
        IIdentityDbContext dbContext,
        ILogger<ReactivateUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(
        ReactivateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.TargetUserId, cancellationToken);

        if (user is null)
        {
            return Result<bool>.Failure("USER_NOT_FOUND");
        }

        if (user.Status == UserStatus.Active)
        {
            return Result<bool>.Failure("ALREADY_ACTIVE");
        }

        user.Status = UserStatus.Active;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin reactivated user {UserId}", command.TargetUserId);

        return Result<bool>.Success(true);
    }
}
