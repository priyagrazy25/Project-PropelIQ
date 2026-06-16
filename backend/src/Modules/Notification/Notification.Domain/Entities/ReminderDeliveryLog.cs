using SharedKernel.Domain;

namespace Notification.Domain.Entities;

public sealed class ReminderDeliveryLog : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string ReminderWindow { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
