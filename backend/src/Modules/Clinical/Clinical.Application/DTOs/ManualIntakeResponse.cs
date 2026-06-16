namespace Clinical.Application.DTOs;

/// <summary>
/// Response DTO for manual intake GET endpoint.
/// </summary>
public sealed record ManualIntakeResponse(
    Guid Id,
    Guid PatientId,
    Guid? AppointmentId,
    string? MedicalHistory,
    string? Symptoms,
    string? Allergies,
    string? CurrentMedications,
    bool IsComplete,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
