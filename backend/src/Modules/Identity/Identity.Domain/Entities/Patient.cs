using SharedKernel.Domain;

namespace Identity.Domain.Entities;

public sealed class Patient : BaseEntity
{
    public Guid UserId { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IntakeCompleted { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
