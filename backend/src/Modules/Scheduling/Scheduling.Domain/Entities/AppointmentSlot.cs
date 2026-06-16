using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class AppointmentSlot : BaseEntity
{
    public Guid ProviderId { get; set; }
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    // Concurrency token for row-level locking (DR-009)
    public byte[] RowVersion { get; set; } = [];

    // Navigation
    public Provider Provider { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}
