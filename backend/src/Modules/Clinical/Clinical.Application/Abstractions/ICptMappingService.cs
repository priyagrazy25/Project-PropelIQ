using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Service for AI-driven CPT code mapping (AIR-005).
/// Maps procedures/encounters from 360-view to CPT codes using NER classification and lookup table.
/// </summary>
public interface ICptMappingService
{
    /// <summary>
    /// Maps patient procedures to CPT codes.
    /// Returns top-3 candidates ranked by confidence.
    /// </summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of CPT mapping results with candidates.</returns>
    Task<Result<IReadOnlyList<CptMappingResult>>> MapProceduresAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of mapping a single procedure to CPT codes.
/// </summary>
public sealed record CptMappingResult(
    Guid ExtractedDataId,
    string SourceProcedure,
    IReadOnlyList<CptCandidate> Candidates,
    bool AllBelowThreshold,
    int TokensUsed);

/// <summary>
/// A single CPT code candidate with confidence score (AC-2).
/// </summary>
public sealed record CptCandidate(
    string Code,
    string Description,
    double Confidence);
