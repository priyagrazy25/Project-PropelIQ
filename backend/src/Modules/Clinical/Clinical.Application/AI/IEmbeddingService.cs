using SharedKernel.Domain;

namespace Clinical.Application.AI;

/// <summary>
/// Abstraction for document embedding generation and storage (AIR-R04).
/// Generates embeddings via Ollama and stores in SQL Server vector table.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates embedding for a text chunk and stores it.
    /// </summary>
    /// <param name="documentId">Parent document ID.</param>
    /// <param name="chunkIndex">Chunk index within document.</param>
    /// <param name="chunkText">Text content to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with embedding ID or error.</returns>
    Task<Result<Guid>> GenerateAndStoreEmbeddingAsync(
        Guid documentId,
        int chunkIndex,
        string chunkText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for all chunks of a document.
    /// </summary>
    /// <param name="documentId">Document to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with count of embeddings generated.</returns>
    Task<Result<EmbeddingBatchResult>> GenerateDocumentEmbeddingsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar documents using cosine similarity search.
    /// </summary>
    /// <param name="queryText">Text to search for similar content.</param>
    /// <param name="topK">Number of results to return.</param>
    /// <param name="patientId">Optional patient ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Similar document chunks ranked by cosine similarity.</returns>
    Task<Result<IReadOnlyList<SimilarChunk>>> FindSimilarAsync(
        string queryText,
        int topK = 5,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if embedding service (Ollama) is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of batch embedding generation.
/// </summary>
/// <param name="DocumentId">Processed document ID.</param>
/// <param name="EmbeddingsGenerated">Number of embeddings created.</param>
/// <param name="TotalTokensUsed">Total tokens consumed.</param>
/// <param name="ProcessingTimeMs">Processing time in milliseconds.</param>
public sealed record EmbeddingBatchResult(
    Guid DocumentId,
    int EmbeddingsGenerated,
    int TotalTokensUsed,
    long ProcessingTimeMs
);

/// <summary>
/// Similar chunk result from cosine similarity search.
/// </summary>
/// <param name="DocumentId">Source document ID.</param>
/// <param name="ChunkIndex">Chunk index within document.</param>
/// <param name="ChunkText">Text content of the chunk.</param>
/// <param name="SimilarityScore">Cosine similarity score (0-1).</param>
public sealed record SimilarChunk(
    Guid DocumentId,
    int ChunkIndex,
    string ChunkText,
    double SimilarityScore
);
