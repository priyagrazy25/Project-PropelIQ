using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class PatientView360 : BaseEntity
{
    public Guid PatientId { get; set; }

    // JSON-structured clinical sections (AC-3, DR-006)
    public PatientVitals? Vitals { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Medications { get; set; }
    public string? Allergies { get; set; }
    public string? LabResults { get; set; }
    public string? Diagnoses { get; set; }

    public DateTime LastRefreshedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PatientVitals
{
    public string? BloodPressure { get; set; }
    public double? HeartRate { get; set; }
    public double? Temperature { get; set; }
    public double? Weight { get; set; }
    public double? Height { get; set; }
    public DateTime? RecordedAt { get; set; }
}
