using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Identity.Application.Commands.Register;

public sealed class RegisterCommandHandler
{
    private readonly IIdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RegisterCommandValidator _validator;

    public RegisterCommandHandler(
        IIdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        RegisterCommandValidator validator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _validator = validator;
    }

    public async Task<Result<RegisterResult>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<RegisterResult>.Failure(errors);
        }

        // Sanitize inputs
        var email = command.Email.Trim().ToLowerInvariant();
        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();

        // Check email uniqueness (EF Core LINQ — parameterized)
        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            return Result<RegisterResult>.Failure("DUPLICATE_EMAIL");
        }

        // Hash password with Argon2id
        var passwordHash = _passwordHasher.Hash(command.Password);

        // Create user entity
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FullName = $"{firstName} {lastName}",
            DateOfBirth = DateOnly.TryParse(command.DateOfBirth, out var dob) ? dob : null,
            ContactNumber = command.Phone.Trim(),
            Role = UserRole.Patient,
            Status = UserStatus.Active,
        };

        // Create associated patient record
        var patient = new Patient
        {
            UserId = user.Id,
        };

        _dbContext.Users.Add(user);
        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<RegisterResult>.Success(new RegisterResult(user.Id));
    }
}
