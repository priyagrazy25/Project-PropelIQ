using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Service for AI-driven ICD-10-CM mapping (AIR-004).
/// Maps diagnoses from 360-view to ICD-10 codes using NER classification and lookup table.
/// </summary>
public interface IIcd10MappingService
{
    /// <summary>
    /// Maps patient diagnoses to ICD-10-CM codes.
    /// Returns top-3 candidates ranked by confidence.
    /// </summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of ICD-10 mapping results with candidates.</returns>
    Task<Result<IReadOnlyList<Icd10MappingResult>>> MapDiagnosesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the code verification queue for staff (SCR-018).
    /// </summary>
    Task<Result<CodeVerificationQueue>> GetVerificationQueueAsync(
        string? statusFilter,
        string? codeTypeFilter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies (accepts or rejects) a code entry.
    /// </summary>
    Task<Result<CodeVerificationResult>> VerifyCodeAsync(
        Guid codeId,
        Guid userId,
        string action,
        string? reason,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of mapping a single diagnosis to ICD-10 codes.
/// </summary>
public sealed record Icd10MappingResult(
    Guid ExtractedDataId,
    string SourceDiagnosis,
    IReadOnlyList<Icd10Candidate> Candidates,
    bool AllBelowThreshold,
    int TokensUsed);

/// <summary>
/// A single ICD-10 code candidate with confidence score (AC-2).
/// </summary>
public sealed record Icd10Candidate(
    string Code,
    string Description,
    double Confidence);

/// <summary>
/// Code verification queue response.
/// </summary>
public sealed record CodeVerificationQueue(
    IReadOnlyList<CodeVerificationEntry> Entries,
    int TotalCount,
    int PendingCount);

/// <summary>
/// Single entry in the verification queue.
/// </summary>
public sealed record CodeVerificationEntry(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string CodeType,
    Icd10Candidate PrimaryCode,
    IReadOnlyList<Icd10Candidate> AlternativeCandidates,
    string Status,
    DateTime ExtractedAt);

/// <summary>
/// Result of verifying a code.
/// </summary>
public sealed record CodeVerificationResult(string Status);
