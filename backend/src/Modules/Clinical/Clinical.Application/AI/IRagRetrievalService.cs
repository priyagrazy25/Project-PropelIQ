using SharedKernel.Domain;

namespace Clinical.Application.AI;

/// <summary>
/// RAG retrieval service for context-aware clinical document search (AIR-R02, AIR-R03).
/// Implements hybrid re-ranking with semantic similarity and recency weighting.
/// </summary>
public interface IRagRetrievalService
{
    /// <summary>
    /// Retrieves relevant document chunks for a query with hybrid re-ranking.
    /// </summary>
    /// <param name="queryText">Search query text.</param>
    /// <param name="patientId">Patient ID to scope the search.</param>
    /// <param name="options">Retrieval options (thresholds, weights).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked chunks with combined scores.</returns>
    Task<Result<RagRetrievalResult>> RetrieveAsync(
        string queryText,
        Guid patientId,
        RagRetrievalOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assembles retrieved chunks into LLM-ready context.
    /// </summary>
    /// <param name="chunks">Retrieved and ranked chunks.</param>
    /// <param name="maxTokens">Maximum tokens for assembled context.</param>
    /// <returns>Assembled context string.</returns>
    string AssembleContext(IReadOnlyList<RankedChunk> chunks, int maxTokens = 4096);
}

/// <summary>
/// Options for RAG retrieval.
/// </summary>
/// <param name="TopK">Number of top results to retrieve.</param>
/// <param name="SimilarityThreshold">Minimum cosine similarity (default 0.75 per AIR-R02).</param>
/// <param name="RecencyWeight">Weight multiplier for newer documents (default 1.2 per AIR-R03).</param>
/// <param name="RecencyWindowDays">Days within which recency boost applies.</param>
public sealed record RagRetrievalOptions(
    int TopK = 5,
    double SimilarityThreshold = 0.75,
    double RecencyWeight = 1.2,
    int RecencyWindowDays = 30
);

/// <summary>
/// Result of RAG retrieval operation.
/// </summary>
/// <param name="Chunks">Retrieved and ranked chunks.</param>
/// <param name="TotalCandidates">Total candidates before filtering.</param>
/// <param name="FilteredCount">Chunks passing similarity threshold.</param>
/// <param name="AssembledContext">Pre-assembled LLM context (optional).</param>
public sealed record RagRetrievalResult(
    IReadOnlyList<RankedChunk> Chunks,
    int TotalCandidates,
    int FilteredCount,
    string? AssembledContext = null
);

/// <summary>
/// A document chunk with hybrid ranking scores.
/// </summary>
/// <param name="DocumentId">Source document ID.</param>
/// <param name="ChunkIndex">Chunk index within document.</param>
/// <param name="ChunkText">Text content of the chunk.</param>
/// <param name="FileName">Source document file name.</param>
/// <param name="SimilarityScore">Cosine similarity score (0-1).</param>
/// <param name="RecencyScore">Recency score (higher for newer docs).</param>
/// <param name="CombinedScore">Final hybrid score.</param>
/// <param name="DocumentCreatedAt">Document upload timestamp.</param>
public sealed record RankedChunk(
    Guid DocumentId,
    int ChunkIndex,
    string ChunkText,
    string FileName,
    double SimilarityScore,
    double RecencyScore,
    double CombinedScore,
    DateTime DocumentCreatedAt
);
