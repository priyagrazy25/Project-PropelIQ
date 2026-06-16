using SharedKernel.Domain;

namespace Notification.Domain.Entities;

/// <summary>
/// Tracks the mapping between an appointment and its external calendar event.
/// </summary>
public sealed class CalendarSyncRecord : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string Provider { get; set; } = string.Empty;       // "Google" or "Outlook"
    public string ExternalEventId { get; set; } = string.Empty;
    public string Status { get; set; } = "Synced";             // Synced, Failed, Deleted
    public string? FailureReason { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
