using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class IntakeRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Appointment? Appointment { get; set; }
}
