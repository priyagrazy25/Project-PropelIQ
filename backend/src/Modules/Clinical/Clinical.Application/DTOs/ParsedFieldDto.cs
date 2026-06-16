namespace Clinical.Application.DTOs;

/// <summary>
/// DTO representing a single parsed field extracted from AI conversation.
/// </summary>
public sealed record ParsedFieldDto(
    string FieldName,
    string Value,
    double ConfidenceScore,
    string Category);
