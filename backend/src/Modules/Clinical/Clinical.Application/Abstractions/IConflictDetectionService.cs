using Clinical.Domain.Entities;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Service for detecting data conflicts across clinical documents (FR-026, AIR-006).
/// Compares extracted data points of the same category, flagging contradictions
/// with severity classification and source document references.
/// </summary>
public interface IConflictDetectionService
{
    /// <summary>
    /// Detects conflicts in extracted data for a patient.
    /// Compares same-category entries across documents, identifying contradictions.
    /// </summary>
    /// <param name="patientId">The patient ID.</param>
    /// <param name="extractedData">The extracted data to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of detected conflicts.</returns>
    Task<IReadOnlyList<DataConflict>> DetectConflictsAsync(
        Guid patientId,
        IReadOnlyList<ExtractedData> extractedData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists newly detected conflicts to the database.
    /// Avoids duplicating conflicts that already exist.
    /// </summary>
    /// <param name="conflicts">The conflicts to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of new conflicts persisted.</returns>
    Task<int> PersistConflictsAsync(
        IReadOnlyList<DataConflict> conflicts,
        CancellationToken cancellationToken = default);
}
