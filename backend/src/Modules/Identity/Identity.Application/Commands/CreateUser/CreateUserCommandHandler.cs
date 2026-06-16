using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Identity.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CreateUserCommandValidator _validator;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IIdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        CreateUserCommandValidator validator,
        ILogger<CreateUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CreateUserResult>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<CreateUserResult>.Failure(errors);
        }

        var email = command.Email.Trim().ToLowerInvariant();

        // Check for duplicate email (include soft-deleted)
        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            return Result<CreateUserResult>.Failure("DUPLICATE_EMAIL");
        }

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
        {
            return Result<CreateUserResult>.Failure("INVALID_ROLE");
        }

        // Generate a temporary password (admin-created accounts require password reset)
        var tempPassword = _passwordHasher.Hash(Guid.NewGuid().ToString());

        var user = new User
        {
            Email = email,
            PasswordHash = tempPassword,
            FullName = $"{command.FirstName.Trim()} {command.LastName.Trim()}",
            ContactNumber = command.Phone?.Trim(),
            Role = role,
            Status = UserStatus.Active,
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin created user {UserId} with role {Role}", user.Id, role);

        return Result<CreateUserResult>.Success(new CreateUserResult(user.Id));
    }
}
