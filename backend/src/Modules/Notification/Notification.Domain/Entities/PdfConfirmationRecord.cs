using SharedKernel.Domain;

namespace Notification.Domain.Entities;

public sealed class PdfConfirmationRecord : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public byte[] PdfContent { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Generated";
    public string? DeliveryStatus { get; set; }
    public string? FailureReason { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
