using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? SlotId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ArrivedAt { get; set; }

    // Concurrency token
    public byte[] RowVersion { get; set; } = [];

    // Navigation
    public Provider Provider { get; set; } = null!;
    public AppointmentSlot? Slot { get; set; }
}
