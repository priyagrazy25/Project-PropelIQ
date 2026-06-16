namespace Clinical.Application.DTOs;

/// <summary>
/// Request DTO for field-level editing after submission (AC-4, FR-017).
/// </summary>
public sealed record IntakeFieldUpdateRequest(
    IReadOnlyList<IntakeFieldEdit> Fields);

/// <summary>
/// Single field edit entry for manual intake patch.
/// </summary>
public sealed record IntakeFieldEdit(
    string FieldName,
    string Value);
