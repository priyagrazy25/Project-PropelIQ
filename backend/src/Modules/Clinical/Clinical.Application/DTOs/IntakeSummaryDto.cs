namespace Clinical.Application.DTOs;

/// <summary>
/// Summary response DTO with categorized intake fields and confidence scores (AC-3).
/// Confidence is null for manual edits.
/// </summary>
public sealed record IntakeSummaryDto(
    Guid AppointmentId,
    string CurrentMode,
    bool IsLocked,
    IReadOnlyList<IntakeSummaryCategory> Categories,
    DateTime GeneratedAt);

/// <summary>
/// A category grouping of intake fields (e.g., MedicalHistory, Symptoms).
/// </summary>
public sealed record IntakeSummaryCategory(
    string CategoryName,
    IReadOnlyList<IntakeSummaryField> Fields);

/// <summary>
/// A single field within a summary category.
/// ConfidenceScore is null for manually entered data.
/// </summary>
public sealed record IntakeSummaryField(
    string FieldName,
    string Value,
    double? ConfidenceScore,
    string Source);
