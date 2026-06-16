using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class Waitlist : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public DateTime PreferredDateStart { get; set; }
    public DateTime PreferredDateEnd { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Active;
    public int Position { get; set; }
    public DateTime? NotifiedAt { get; set; }

    // Navigation
    public Provider Provider { get; set; } = null!;
}
