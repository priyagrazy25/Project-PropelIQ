namespace Clinical.Application.DTOs;

/// <summary>
/// Request DTO for manual intake autosave and submit (AC-1, AC-2).
/// Structured fields for history, symptoms, allergies, medications.
/// </summary>
public sealed record ManualIntakeRequest(
    Guid PatientId,
    string? MedicalHistory,
    string? Symptoms,
    string? Allergies,
    string? CurrentMedications);
