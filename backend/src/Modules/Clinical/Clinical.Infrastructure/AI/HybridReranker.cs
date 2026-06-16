using Clinical.Application.AI;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Hybrid re-ranker combining semantic similarity with recency weighting (AIR-R03).
/// Newer documents receive a 1.2x boost to prioritize recent clinical information.
/// </summary>
public sealed class HybridReranker
{
    /// <summary>
    /// Re-ranks chunks using hybrid scoring: semantic similarity + recency.
    /// </summary>
    /// <param name="chunks">Chunks with similarity scores.</param>
    /// <param name="options">Re-ranking options.</param>
    /// <returns>Re-ranked chunks with combined scores.</returns>
    public IReadOnlyList<RankedChunk> Rerank(
        IReadOnlyList<RetrievedChunkWithMetadata> chunks,
        RagRetrievalOptions options)
    {
        if (chunks.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var recencyWindowStart = now.AddDays(-options.RecencyWindowDays);

        return chunks
            .Select(chunk =>
            {
                var recencyScore = CalculateRecencyScore(
                    chunk.DocumentCreatedAt,
                    recencyWindowStart,
                    now,
                    options.RecencyWeight);

                // Combined score: semantic * recency
                // Recency score is 1.0 for old docs, up to RecencyWeight for newest
                var combinedScore = chunk.SimilarityScore * recencyScore;

                return new RankedChunk(
                    DocumentId: chunk.DocumentId,
                    ChunkIndex: chunk.ChunkIndex,
                    ChunkText: chunk.ChunkText,
                    FileName: chunk.FileName,
                    SimilarityScore: chunk.SimilarityScore,
                    RecencyScore: recencyScore,
                    CombinedScore: combinedScore,
                    DocumentCreatedAt: chunk.DocumentCreatedAt);
            })
            .OrderByDescending(c => c.CombinedScore)
            .Take(options.TopK)
            .ToList();
    }

    /// <summary>
    /// Calculates recency score based on document age.
    /// Documents within the recency window get boosted up to RecencyWeight (1.2x).
    /// Older documents get a base score of 1.0.
    /// </summary>
    private static double CalculateRecencyScore(
        DateTime documentCreatedAt,
        DateTime windowStart,
        DateTime now,
        double recencyWeight)
    {
        // If document is older than window, base score of 1.0
        if (documentCreatedAt < windowStart)
        {
            return 1.0;
        }

        // Linear interpolation: newer = higher score
        // At now: recencyWeight (e.g., 1.2)
        // At windowStart: 1.0
        var totalWindowMs = (now - windowStart).TotalMilliseconds;
        var docAgeFromWindowStartMs = (documentCreatedAt - windowStart).TotalMilliseconds;

        // How far through the window (0.0 = at window start, 1.0 = now)
        var progressThroughWindow = docAgeFromWindowStartMs / totalWindowMs;

        // Interpolate between 1.0 and recencyWeight
        return 1.0 + (progressThroughWindow * (recencyWeight - 1.0));
    }
}

/// <summary>
/// Intermediate chunk representation with document metadata for re-ranking.
/// </summary>
/// <param name="DocumentId">Document ID.</param>
/// <param name="ChunkIndex">Chunk index.</param>
/// <param name="ChunkText">Chunk text content.</param>
/// <param name="FileName">Source document filename.</param>
/// <param name="SimilarityScore">Cosine similarity score.</param>
/// <param name="DocumentCreatedAt">Document creation timestamp.</param>
public sealed record RetrievedChunkWithMetadata(
    Guid DocumentId,
    int ChunkIndex,
    string ChunkText,
    string FileName,
    double SimilarityScore,
    DateTime DocumentCreatedAt
);
