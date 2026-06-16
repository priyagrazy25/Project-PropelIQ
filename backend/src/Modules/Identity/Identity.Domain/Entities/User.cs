using Identity.Domain.Enums;
using SharedKernel.Domain;

namespace Identity.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? ContactNumber { get; set; }
    public string? Address { get; set; }
    public UserRole Role { get; set; } = UserRole.Patient;
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;

    // Navigation
    public Patient? Patient { get; set; }

    // Concurrency token for optimistic locking
    public byte[] RowVersion { get; set; } = [];
}
