using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Identity.Application.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly UpdateUserCommandValidator _validator;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IIdentityDbContext dbContext,
        UpdateUserCommandValidator validator,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UpdateUserResult>> HandleAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<UpdateUserResult>.Failure(errors);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UpdateUserResult>.Failure("USER_NOT_FOUND");
        }

        var email = command.Email.Trim().ToLowerInvariant();

        // Check for duplicate email (exclude the current user, include soft-deleted)
        var emailTaken = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && u.Id != command.UserId, cancellationToken);

        if (emailTaken)
        {
            return Result<UpdateUserResult>.Failure("DUPLICATE_EMAIL");
        }

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
        {
            return Result<UpdateUserResult>.Failure("INVALID_ROLE");
        }

        var previousRole = user.Role;

        user.FullName = $"{command.FirstName.Trim()} {command.LastName.Trim()}";
        user.Email = email;
        user.ContactNumber = command.Phone?.Trim();
        user.Role = role;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin updated user {UserId}. Role changed from {OldRole} to {NewRole}",
            user.Id, previousRole, role);

        return Result<UpdateUserResult>.Success(new UpdateUserResult(user.Id));
    }
}
