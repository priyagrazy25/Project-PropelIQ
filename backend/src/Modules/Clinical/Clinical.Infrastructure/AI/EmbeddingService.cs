using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Clinical.Application.AI;
using Clinical.Domain.Entities;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Embedding service using Ollama API for vector generation (AIR-R04).
/// Stores embeddings in SQL Server with cosine similarity search support.
/// Enforces 8,192 token budget per request (AIR-O01).
/// </summary>
public sealed class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ClinicalDbContext _dbContext;
    private readonly EmbeddingCircuitBreaker _circuitBreaker;
    private readonly OllamaOptions _options;
    private readonly ILogger<EmbeddingService> _logger;

    private const int MaxTokenBudget = 8192;
    private const int EmbeddingDimension = 3072; // phi3:mini embedding dimension

    public EmbeddingService(
        HttpClient httpClient,
        ClinicalDbContext dbContext,
        EmbeddingCircuitBreaker circuitBreaker,
        IOptions<OllamaOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _circuitBreaker = circuitBreaker;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<Guid>> GenerateAndStoreEmbeddingAsync(
        Guid documentId,
        int chunkIndex,
        string chunkText,
        CancellationToken cancellationToken = default)
    {
        // Token budget enforcement (AIR-O01)
        var estimatedTokens = EstimateTokenCount(chunkText);
        if (estimatedTokens > MaxTokenBudget)
        {
            _logger.LogWarning(
                "Token budget exceeded for embedding: {Estimated} > {Budget}",
                estimatedTokens, MaxTokenBudget);

            return Result<Guid>.Failure($"Token budget exceeded: {estimatedTokens} > {MaxTokenBudget}");
        }

        try
        {
            // Generate embedding via Ollama
            var embedding = await _circuitBreaker.ExecuteAsync(
                async ct => await GenerateEmbeddingAsync(chunkText, ct),
                cancellationToken);

            if (embedding is null || embedding.Length == 0)
            {
                return Result<Guid>.Failure("Ollama returned empty embedding.");
            }

            // Check for existing embedding
            var existing = await _dbContext.DocumentEmbeddings
                .FirstOrDefaultAsync(e => e.DocumentId == documentId && e.ChunkIndex == chunkIndex, cancellationToken);

            if (existing is not null)
            {
                // Update existing embedding
                existing.ChunkText = chunkText;
                existing.EmbeddingVector = JsonSerializer.Serialize(embedding);
                existing.VectorDimension = embedding.Length;
            }
            else
            {
                // Create new embedding
                var documentEmbedding = new DocumentEmbedding
                {
                    DocumentId = documentId,
                    ChunkIndex = chunkIndex,
                    ChunkText = chunkText,
                    EmbeddingVector = JsonSerializer.Serialize(embedding),
                    VectorDimension = embedding.Length
                };

                _dbContext.Add(documentEmbedding);
                existing = documentEmbedding;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Stored embedding for document {DocumentId} chunk {ChunkIndex} ({Dimension}D)",
                documentId, chunkIndex, embedding.Length);

            return Result<Guid>.Success(existing.Id);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Embedding circuit breaker is open.");
            return Result<Guid>.Failure("Embedding service temporarily unavailable (circuit breaker open).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for document {DocumentId} chunk {ChunkIndex}", documentId, chunkIndex);
            return Result<Guid>.Failure($"Embedding generation failed: {ex.Message}");
        }
    }

    public async Task<Result<EmbeddingBatchResult>> GenerateDocumentEmbeddingsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Load document chunks
        var chunks = await _dbContext.Set<DocumentChunk>()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return Result<EmbeddingBatchResult>.Failure("No chunks found for document.");
        }

        _logger.LogInformation("Generating embeddings for {ChunkCount} chunks of document {DocumentId}", chunks.Count, documentId);

        var embeddingsGenerated = 0;
        var totalTokensUsed = 0;

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await GenerateAndStoreEmbeddingAsync(
                documentId,
                chunk.ChunkIndex,
                chunk.Content,
                cancellationToken);

            if (result.IsSuccess)
            {
                embeddingsGenerated++;
                totalTokensUsed += EstimateTokenCount(chunk.Content);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to generate embedding for chunk {ChunkIndex}: {Error}",
                    chunk.ChunkIndex, result.Error);
            }
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Generated {Count} embeddings for document {DocumentId} in {ElapsedMs}ms",
            embeddingsGenerated, documentId, stopwatch.ElapsedMilliseconds);

        return Result<EmbeddingBatchResult>.Success(new EmbeddingBatchResult(
            DocumentId: documentId,
            EmbeddingsGenerated: embeddingsGenerated,
            TotalTokensUsed: totalTokensUsed,
            ProcessingTimeMs: stopwatch.ElapsedMilliseconds
        ));
    }

    public async Task<Result<IReadOnlyList<SimilarChunk>>> FindSimilarAsync(
        string queryText,
        int topK = 5,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate embedding for query
            var queryEmbedding = await _circuitBreaker.ExecuteAsync(
                async ct => await GenerateEmbeddingAsync(queryText, ct),
                cancellationToken);

            if (queryEmbedding is null || queryEmbedding.Length == 0)
            {
                return Result<IReadOnlyList<SimilarChunk>>.Failure("Failed to generate query embedding.");
            }

            // Load candidate embeddings
            var query = _dbContext.DocumentEmbeddings.AsQueryable();

            if (patientId.HasValue)
            {
                query = query.Where(e => e.Document.PatientId == patientId.Value);
            }

            var embeddings = await query
                .Include(e => e.Document)
                .ToListAsync(cancellationToken);

            // Calculate cosine similarity and rank
            var results = embeddings
                .Select(e =>
                {
                    var vector = JsonSerializer.Deserialize<float[]>(e.EmbeddingVector) ?? [];
                    var similarity = CosineSimilarity(queryEmbedding, vector);
                    return new SimilarChunk(e.DocumentId, e.ChunkIndex, e.ChunkText, similarity);
                })
                .OrderByDescending(s => s.SimilarityScore)
                .Take(topK)
                .ToList();

            return Result<IReadOnlyList<SimilarChunk>>.Success(results);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Embedding circuit breaker is open during similarity search.");
            return Result<IReadOnlyList<SimilarChunk>>.Failure("Embedding service temporarily unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Similarity search failed.");
            return Result<IReadOnlyList<SimilarChunk>>.Failure($"Similarity search failed: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var request = new EmbeddingRequest(_options.ModelId, text);
        var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        return result?.Embedding;
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0.0;
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0.0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Rough token estimation (~4 chars per token for English text).
    /// </summary>
    private static int EstimateTokenCount(string text) => (text.Length + 3) / 4;

    // Ollama API DTOs
    private sealed record EmbeddingRequest(string Model, string Prompt);
    private sealed record EmbeddingResponse(float[]? Embedding);
}
