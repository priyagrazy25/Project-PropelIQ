namespace Clinical.Application.DTOs;

/// <summary>
/// Response DTO representing an intake session.
/// </summary>
public sealed record IntakeSessionDto(
    Guid SessionId,
    Guid PatientId,
    Guid? AppointmentId,
    string Status,
    IReadOnlyList<ParsedFieldDto> ParsedFields,
    DateTime CreatedAt,
    DateTime? CompletedAt);
