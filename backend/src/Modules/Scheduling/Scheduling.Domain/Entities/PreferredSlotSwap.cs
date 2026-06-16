using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class PreferredSlotSwap : BaseEntity
{
    public Guid RequestingPatientId { get; set; }
    public Guid OriginalAppointmentId { get; set; }
    public Guid DesiredSlotId { get; set; }
    public SwapStatus Status { get; set; } = SwapStatus.Pending;
    public int Priority { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Appointment OriginalAppointment { get; set; } = null!;
    public AppointmentSlot DesiredSlot { get; set; } = null!;
}
