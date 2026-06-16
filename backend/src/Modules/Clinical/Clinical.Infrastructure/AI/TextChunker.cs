using System.Text;
using System.Text.RegularExpressions;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Text chunker for RAG retrieval with 512-token segments and 51-token overlap (AIR-R01).
/// Uses whitespace tokenization approximation for clinical text.
/// </summary>
public sealed partial class TextChunker
{
    private const int DefaultChunkSize = 512;
    private const int DefaultOverlap = 51;

    private readonly int _chunkSize;
    private readonly int _overlap;

    public TextChunker(int chunkSize = DefaultChunkSize, int overlap = DefaultOverlap)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive.");
        if (overlap < 0 || overlap >= chunkSize)
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be non-negative and less than chunk size.");

        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    // Split on whitespace for tokenization
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();

    /// <summary>
    /// Splits text into overlapping chunks.
    /// </summary>
    /// <param name="text">Input text to chunk.</param>
    /// <returns>List of text chunks with metadata.</returns>
    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var tokens = WhitespacePattern().Split(text.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();

        if (tokens.Length == 0)
        {
            return [];
        }

        var chunks = new List<TextChunk>();
        var stride = _chunkSize - _overlap;
        var chunkIndex = 0;

        for (var i = 0; i < tokens.Length; i += stride)
        {
            var endIndex = Math.Min(i + _chunkSize, tokens.Length);
            var chunkTokens = tokens[i..endIndex];

            var content = string.Join(" ", chunkTokens);
            chunks.Add(new TextChunk(
                Index: chunkIndex,
                Content: content,
                TokenCount: chunkTokens.Length,
                StartTokenIndex: i,
                EndTokenIndex: endIndex - 1
            ));

            chunkIndex++;

            // Stop if we've reached the end
            if (endIndex >= tokens.Length)
            {
                break;
            }
        }

        return chunks;
    }

    /// <summary>
    /// Chunks text from multiple pages, preserving page source information.
    /// </summary>
    /// <param name="pageTexts">Dictionary of page number to text content.</param>
    /// <returns>List of chunks with page source tracking.</returns>
    public IReadOnlyList<PagedTextChunk> ChunkWithPages(IReadOnlyDictionary<int, string> pageTexts)
    {
        if (pageTexts.Count == 0)
        {
            return [];
        }

        // Build token list with page tracking
        var allTokens = new List<(string Token, int PageNumber)>();

        foreach (var (pageNumber, text) in pageTexts.OrderBy(p => p.Key))
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var tokens = WhitespacePattern().Split(text.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            foreach (var token in tokens)
            {
                allTokens.Add((token, pageNumber));
            }
        }

        if (allTokens.Count == 0)
        {
            return [];
        }

        var chunks = new List<PagedTextChunk>();
        var stride = _chunkSize - _overlap;
        var chunkIndex = 0;

        for (var i = 0; i < allTokens.Count; i += stride)
        {
            var endIndex = Math.Min(i + _chunkSize, allTokens.Count);
            var chunkTokens = allTokens.Skip(i).Take(endIndex - i).ToList();

            var content = string.Join(" ", chunkTokens.Select(t => t.Token));
            var sourcePages = chunkTokens.Select(t => t.PageNumber).Distinct().OrderBy(p => p).ToArray();

            chunks.Add(new PagedTextChunk(
                Index: chunkIndex,
                Content: content,
                TokenCount: chunkTokens.Count,
                SourcePages: sourcePages
            ));

            chunkIndex++;

            if (endIndex >= allTokens.Count)
            {
                break;
            }
        }

        return chunks;
    }
}

/// <summary>
/// Represents a text chunk with metadata.
/// </summary>
/// <param name="Index">Zero-based chunk index.</param>
/// <param name="Content">Text content of the chunk.</param>
/// <param name="TokenCount">Number of tokens in this chunk.</param>
/// <param name="StartTokenIndex">Starting token index in original text.</param>
/// <param name="EndTokenIndex">Ending token index in original text.</param>
public sealed record TextChunk(
    int Index,
    string Content,
    int TokenCount,
    int StartTokenIndex,
    int EndTokenIndex
);

/// <summary>
/// Represents a text chunk with page source information.
/// </summary>
/// <param name="Index">Zero-based chunk index.</param>
/// <param name="Content">Text content of the chunk.</param>
/// <param name="TokenCount">Number of tokens in this chunk.</param>
/// <param name="SourcePages">Array of page numbers this chunk spans.</param>
public sealed record PagedTextChunk(
    int Index,
    string Content,
    int TokenCount,
    int[] SourcePages
);
