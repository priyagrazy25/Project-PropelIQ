using Clinical.Domain.Enums;

namespace Clinical.Application.DTOs;

/// <summary>
/// Full 360-degree patient view response (SCR-016, AIR-002).
/// Returns aggregated clinical data with confidence scores and sources.
/// </summary>
public sealed record PatientView360Response(
    PatientDemographicsDto Demographics,
    IReadOnlyList<ExtractedDataPointDto> ExtractedData,
    IReadOnlyList<ConflictSummaryDto> Conflicts,
    int DocumentCount,
    DateTime LastUpdated);

/// <summary>
/// Patient demographics for the 360 view banner.
/// </summary>
public sealed record PatientDemographicsDto(
    Guid PatientId,
    string FullName,
    string DateOfBirth,
    string Gender,
    string Mrn,
    string? Phone,
    string? Email,
    string? Address,
    string? InsuranceProvider,
    string? MemberId);

/// <summary>
/// Extracted data point with confidence score (UXR-107, AC-3).
/// </summary>
public sealed record ExtractedDataPointDto(
    Guid Id,
    string Category,
    string Field,
    string Value,
    string SourceDocument,
    double Confidence,
    DateTime ExtractedAt,
    Guid? ConflictId);

/// <summary>
/// Conflict summary for alert display (AC-8).
/// </summary>
public sealed record ConflictSummaryDto(
    Guid ConflictId,
    string Field,
    string Category,
    IReadOnlyList<string> Values,
    string Severity);
