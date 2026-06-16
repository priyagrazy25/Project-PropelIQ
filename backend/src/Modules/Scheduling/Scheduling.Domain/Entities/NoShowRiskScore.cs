using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class NoShowRiskScore : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public int Score { get; set; }
    public string? RiskFactors { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Appointment Appointment { get; set; } = null!;
}
