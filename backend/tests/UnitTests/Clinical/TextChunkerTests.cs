using Clinical.Infrastructure.AI;

namespace UnitTests.Clinical;

/// <summary>
/// Unit tests for TextChunker verifying 512-token segmentation with 51-token overlap (AIR-R01).
/// </summary>
public sealed class TextChunkerTests
{
    [Fact]
    public void Chunk_EmptyString_ReturnsEmptyList()
    {
        var chunker = new TextChunker();

        var result = chunker.Chunk(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_WhitespaceOnly_ReturnsEmptyList()
    {
        var chunker = new TextChunker();

        var result = chunker.Chunk("   \t\n   ");

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var chunker = new TextChunker(chunkSize: 512, overlap: 51);
        var input = string.Join(" ", Enumerable.Repeat("word", 100));

        var result = chunker.Chunk(input);

        Assert.Single(result);
        Assert.Equal(0, result[0].Index);
        Assert.Equal(100, result[0].TokenCount);
    }

    [Fact]
    public void Chunk_ExactChunkSize_ReturnsSingleChunk()
    {
        var chunker = new TextChunker(chunkSize: 512, overlap: 51);
        var input = string.Join(" ", Enumerable.Repeat("word", 512));

        var result = chunker.Chunk(input);

        Assert.Single(result);
        Assert.Equal(512, result[0].TokenCount);
    }

    [Fact]
    public void Chunk_OverChunkSize_ReturnsMultipleChunks()
    {
        var chunker = new TextChunker(chunkSize: 100, overlap: 10);
        var input = string.Join(" ", Enumerable.Repeat("word", 250));

        var result = chunker.Chunk(input);

        Assert.True(result.Count > 1);
    }

    [Fact]
    public void Chunk_WithOverlap_ChunksOverlap()
    {
        var chunker = new TextChunker(chunkSize: 100, overlap: 20);
        var words = Enumerable.Range(1, 200).Select(i => $"word{i}").ToArray();
        var input = string.Join(" ", words);

        var result = chunker.Chunk(input);

        Assert.True(result.Count >= 2);

        // Second chunk should start at index 80 (100 - 20 overlap)
        var firstChunkEnd = result[0].EndTokenIndex;
        var secondChunkStart = result[1].StartTokenIndex;
        Assert.Equal(80, secondChunkStart);
    }

    [Fact]
    public void Chunk_ChunkIndicesAreSequential()
    {
        var chunker = new TextChunker(chunkSize: 50, overlap: 5);
        var input = string.Join(" ", Enumerable.Repeat("word", 200));

        var result = chunker.Chunk(input);

        for (var i = 0; i < result.Count; i++)
        {
            Assert.Equal(i, result[i].Index);
        }
    }

    [Fact]
    public void Chunk_DefaultParameters_Use512And51()
    {
        var chunker = new TextChunker(); // Defaults
        var input = string.Join(" ", Enumerable.Repeat("word", 600));

        var result = chunker.Chunk(input);

        // First chunk should have 512 tokens
        Assert.Equal(512, result[0].TokenCount);
        // Second chunk should start at 461 (512 - 51)
        Assert.Equal(461, result[1].StartTokenIndex);
    }

    [Fact]
    public void Constructor_InvalidChunkSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextChunker(chunkSize: 0, overlap: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextChunker(chunkSize: -1, overlap: 10));
    }

    [Fact]
    public void Constructor_OverlapGreaterThanChunkSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextChunker(chunkSize: 50, overlap: 100));
    }

    [Fact]
    public void Constructor_NegativeOverlap_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextChunker(chunkSize: 50, overlap: -1));
    }

    [Fact]
    public void ChunkWithPages_EmptyDictionary_ReturnsEmptyList()
    {
        var chunker = new TextChunker();

        var result = chunker.ChunkWithPages(new Dictionary<int, string>());

        Assert.Empty(result);
    }

    [Fact]
    public void ChunkWithPages_SinglePage_TracksPageSource()
    {
        var chunker = new TextChunker(chunkSize: 100, overlap: 10);
        var pageTexts = new Dictionary<int, string>
        {
            { 1, string.Join(" ", Enumerable.Repeat("word", 50)) }
        };

        var result = chunker.ChunkWithPages(pageTexts);

        Assert.Single(result);
        Assert.Contains(1, result[0].SourcePages);
    }

    [Fact]
    public void ChunkWithPages_MultiplePages_TracksAllPageSources()
    {
        var chunker = new TextChunker(chunkSize: 100, overlap: 10);
        var pageTexts = new Dictionary<int, string>
        {
            { 1, string.Join(" ", Enumerable.Repeat("page1", 60)) },
            { 2, string.Join(" ", Enumerable.Repeat("page2", 60)) }
        };

        var result = chunker.ChunkWithPages(pageTexts);

        // Should have chunks spanning both pages
        Assert.True(result.Count >= 1);
        // At least one chunk should reference page 1
        Assert.True(result.Any(c => c.SourcePages.Contains(1)));
    }

    [Fact]
    public void ChunkWithPages_PagesOrderedCorrectly()
    {
        var chunker = new TextChunker(chunkSize: 50, overlap: 5);
        var pageTexts = new Dictionary<int, string>
        {
            { 3, "third page content" },
            { 1, "first page content" },
            { 2, "second page content" }
        };

        var result = chunker.ChunkWithPages(pageTexts);

        // Content should be combined in page order (1, 2, 3)
        var combinedContent = string.Join(" ", result.Select(c => c.Content));
        var firstIdx = combinedContent.IndexOf("first", StringComparison.Ordinal);
        var secondIdx = combinedContent.IndexOf("second", StringComparison.Ordinal);
        var thirdIdx = combinedContent.IndexOf("third", StringComparison.Ordinal);

        Assert.True(firstIdx < secondIdx);
        Assert.True(secondIdx < thirdIdx);
    }

    [Fact]
    public void AIR_R01_Compliance_512TokenChunksWithTenPercentOverlap()
    {
        // AIR-R01 specifies 512 token chunks with 10% overlap (51 tokens)
        var chunker = new TextChunker(chunkSize: 512, overlap: 51);

        // Create text longer than 512 tokens
        var input = string.Join(" ", Enumerable.Repeat("clinical", 1000));

        var result = chunker.Chunk(input);

        // First chunk should be 512 tokens
        Assert.Equal(512, result[0].TokenCount);

        // Stride should be 461 (512 - 51)
        Assert.Equal(0, result[0].StartTokenIndex);
        Assert.Equal(461, result[1].StartTokenIndex);
    }
}
