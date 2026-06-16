using System.Diagnostics;
using System.Text.Json;
using Clinical.Application.AI;
using Clinical.Domain.Entities;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// RAG retrieval service implementing hybrid re-ranking (AIR-R02, AIR-R03).
/// Retrieves top-5 chunks with cosine similarity ≥ 0.75 and recency weighting.
/// </summary>
public sealed class RagRetrievalService : IRagRetrievalService
{
    private readonly ClinicalDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly HybridReranker _reranker;
    private readonly ContextAssembler _contextAssembler;
    private readonly ILogger<RagRetrievalService> _logger;

    public RagRetrievalService(
        ClinicalDbContext dbContext,
        IEmbeddingService embeddingService,
        HybridReranker reranker,
        ContextAssembler contextAssembler,
        ILogger<RagRetrievalService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _reranker = reranker;
        _contextAssembler = contextAssembler;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<RagRetrievalResult>> RetrieveAsync(
        string queryText,
        Guid patientId,
        RagRetrievalOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new RagRetrievalOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                "Starting RAG retrieval for patient {PatientId}: '{Query}' (top-{K}, threshold={Threshold})",
                patientId, TruncateForLog(queryText), options.TopK, options.SimilarityThreshold);

            // Step 1: Generate query embedding
            var similarResult = await _embeddingService.FindSimilarAsync(
                queryText,
                topK: options.TopK * 3, // Retrieve more candidates for filtering
                patientId: patientId,
                cancellationToken: cancellationToken);

            if (!similarResult.IsSuccess)
            {
                return Result<RagRetrievalResult>.Failure(similarResult.Error!);
            }

            var candidates = similarResult.Value!;
            var totalCandidates = candidates.Count;

            if (totalCandidates == 0)
            {
                _logger.LogInformation("No embeddings found for patient {PatientId}", patientId);
                return Result<RagRetrievalResult>.Success(new RagRetrievalResult(
                    Chunks: [],
                    TotalCandidates: 0,
                    FilteredCount: 0,
                    AssembledContext: _contextAssembler.AssembleEmptyContext()));
            }

            // Step 2: Filter by similarity threshold (AIR-R02: ≥ 0.75)
            var filteredChunks = candidates
                .Where(c => c.SimilarityScore >= options.SimilarityThreshold)
                .ToList();

            var filteredCount = filteredChunks.Count;

            if (filteredCount == 0)
            {
                _logger.LogInformation(
                    "No chunks passed threshold {Threshold} for patient {PatientId}",
                    options.SimilarityThreshold, patientId);

                return Result<RagRetrievalResult>.Success(new RagRetrievalResult(
                    Chunks: [],
                    TotalCandidates: totalCandidates,
                    FilteredCount: 0,
                    AssembledContext: _contextAssembler.AssembleEmptyContext()));
            }

            // Step 3: Load document metadata for recency scoring
            var documentIds = filteredChunks.Select(c => c.DocumentId).Distinct().ToList();
            var documents = await _dbContext.ClinicalDocuments
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d, cancellationToken);

            // Step 4: Build chunks with metadata
            var chunksWithMetadata = filteredChunks
                .Select(c =>
                {
                    var doc = documents.GetValueOrDefault(c.DocumentId);
                    return new RetrievedChunkWithMetadata(
                        DocumentId: c.DocumentId,
                        ChunkIndex: c.ChunkIndex,
                        ChunkText: c.ChunkText,
                        FileName: doc?.FileName ?? "Unknown",
                        SimilarityScore: c.SimilarityScore,
                        DocumentCreatedAt: doc?.CreatedAt ?? DateTime.MinValue);
                })
                .ToList();

            // Step 5: Apply hybrid re-ranking (AIR-R03: recency 1.2x)
            var rankedChunks = _reranker.Rerank(chunksWithMetadata, options);

            // Step 6: Assemble context
            var assembledContext = _contextAssembler.Assemble(rankedChunks);

            stopwatch.Stop();

            _logger.LogInformation(
                "RAG retrieval complete: {FilteredCount}/{TotalCandidates} chunks, top-{TopK} returned in {ElapsedMs}ms",
                filteredCount, totalCandidates, rankedChunks.Count, stopwatch.ElapsedMilliseconds);

            return Result<RagRetrievalResult>.Success(new RagRetrievalResult(
                Chunks: rankedChunks,
                TotalCandidates: totalCandidates,
                FilteredCount: filteredCount,
                AssembledContext: assembledContext));
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "RAG retrieval circuit breaker open");
            return Result<RagRetrievalResult>.Failure("Embedding service temporarily unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG retrieval failed for patient {PatientId}", patientId);
            return Result<RagRetrievalResult>.Failure($"Retrieval failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public string AssembleContext(IReadOnlyList<RankedChunk> chunks, int maxTokens = 4096)
    {
        return _contextAssembler.Assemble(chunks, maxTokens);
    }

    /// <summary>
    /// Truncates text for logging purposes.
    /// </summary>
    private static string TruncateForLog(string text, int maxLength = 50)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }
        return text[..(maxLength - 3)] + "...";
    }
}
