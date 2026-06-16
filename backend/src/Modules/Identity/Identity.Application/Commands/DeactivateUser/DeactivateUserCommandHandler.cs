using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Identity.Application.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;

    public DeactivateUserCommandHandler(
        IIdentityDbContext dbContext,
        ILogger<DeactivateUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(
        DeactivateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TargetUserId == command.AdminUserId)
        {
            return Result<bool>.Failure("SELF_DEACTIVATION");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.TargetUserId, cancellationToken);

        if (user is null)
        {
            return Result<bool>.Failure("USER_NOT_FOUND");
        }

        if (user.Status == UserStatus.Inactive)
        {
            return Result<bool>.Failure("ALREADY_INACTIVE");
        }

        user.Status = UserStatus.Inactive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminId} deactivated user {UserId}",
            command.AdminUserId, command.TargetUserId);

        return Result<bool>.Success(true);
    }
}
