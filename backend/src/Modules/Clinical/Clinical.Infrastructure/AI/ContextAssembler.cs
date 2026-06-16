using System.Text;
using Clinical.Application.AI;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Assembles retrieved chunks into LLM-ready context with source citations.
/// Formats context for optimal RAG generation while respecting token limits.
/// </summary>
public sealed class ContextAssembler
{
    private const string ContextHeader = """
        Based on the following clinical document excerpts, provide accurate information.
        Each excerpt includes the source document and relevance score.

        CLINICAL CONTEXT:
        ================

        """;

    private const string ContextFooter = """

        ================
        END OF CONTEXT

        Instructions: Use only the information provided above. Cite source documents when possible.
        If the context doesn't contain sufficient information, acknowledge the limitation.
        """;

    /// <summary>
    /// Assembles ranked chunks into a formatted context string.
    /// </summary>
    /// <param name="chunks">Ranked chunks to assemble.</param>
    /// <param name="maxTokens">Maximum token budget for context.</param>
    /// <returns>Assembled context string.</returns>
    public string Assemble(IReadOnlyList<RankedChunk> chunks, int maxTokens = 4096)
    {
        if (chunks.Count == 0)
        {
            return AssembleEmptyContext();
        }

        var builder = new StringBuilder();
        builder.Append(ContextHeader);

        var currentTokenEstimate = EstimateTokens(ContextHeader) + EstimateTokens(ContextFooter);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var formattedChunk = FormatChunk(chunk, i + 1);
            var chunkTokens = EstimateTokens(formattedChunk);

            // Check if adding this chunk would exceed budget
            if (currentTokenEstimate + chunkTokens > maxTokens)
            {
                // Try to fit remaining space with truncated content
                var remainingTokens = maxTokens - currentTokenEstimate - 50; // Buffer for truncation marker
                if (remainingTokens > 100)
                {
                    var truncatedChunk = TruncateChunk(chunk, i + 1, remainingTokens);
                    builder.Append(truncatedChunk);
                }
                break;
            }

            builder.Append(formattedChunk);
            currentTokenEstimate += chunkTokens;
        }

        builder.Append(ContextFooter);

        return builder.ToString();
    }

    /// <summary>
    /// Creates context response when no relevant chunks are found.
    /// </summary>
    public string AssembleEmptyContext()
    {
        return """
            No relevant clinical context found for this query.
            The patient's documents do not contain information matching the search criteria.
            Please verify the query or check if relevant documents have been uploaded.
            """;
    }

    /// <summary>
    /// Formats a single chunk with metadata for context inclusion.
    /// </summary>
    private static string FormatChunk(RankedChunk chunk, int index)
    {
        return $"""

            [{index}] Source: {chunk.FileName} (Relevance: {chunk.CombinedScore:P0})
            Date: {chunk.DocumentCreatedAt:yyyy-MM-dd}
            ---
            {chunk.ChunkText.Trim()}
            ---

            """;
    }

    /// <summary>
    /// Creates a truncated chunk to fit within remaining token budget.
    /// </summary>
    private static string TruncateChunk(RankedChunk chunk, int index, int maxTokens)
    {
        // Rough estimate: 4 chars per token
        var maxChars = maxTokens * 4;
        var truncatedText = chunk.ChunkText.Length > maxChars
            ? chunk.ChunkText[..(maxChars - 20)] + "... [truncated]"
            : chunk.ChunkText;

        return $"""

            [{index}] Source: {chunk.FileName} (Relevance: {chunk.CombinedScore:P0})
            Date: {chunk.DocumentCreatedAt:yyyy-MM-dd}
            ---
            {truncatedText.Trim()}
            ---

            """;
    }

    /// <summary>
    /// Estimates token count for text (~4 chars per token).
    /// </summary>
    private static int EstimateTokens(string text) => (text.Length + 3) / 4;
}
