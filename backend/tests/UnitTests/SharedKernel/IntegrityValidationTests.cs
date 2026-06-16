using SharedKernel.Domain;
using SharedKernel.ValueObjects;

namespace UnitTests.SharedKernel;

public class IntegrityValidationTests
{
    // --- BaseEntity soft-delete properties ---

    [Fact]
    public void BaseEntity_IsDeleted_DefaultsFalse()
    {
        var entity = new TestEntity();

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
    }

    [Fact]
    public void BaseEntity_SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var entity = new TestEntity();
        var deletedAt = DateTime.UtcNow;

        entity.IsDeleted = true;
        entity.DeletedAt = deletedAt;

        Assert.True(entity.IsDeleted);
        Assert.Equal(deletedAt, entity.DeletedAt);
    }

    [Fact]
    public void BaseEntity_SoftDelete_PreservesId()
    {
        var entity = new TestEntity();
        var originalId = entity.Id;

        entity.IsDeleted = true;

        Assert.Equal(originalId, entity.Id);
    }

    // --- ConfidenceScore value object ---

    [Fact]
    public void ConfidenceScore_ValidValue_CreatesSuccessfully()
    {
        var score = new ConfidenceScore(0.85);

        Assert.Equal(0.85, score.Value);
    }

    [Fact]
    public void ConfidenceScore_ZeroBoundary_CreatesSuccessfully()
    {
        var score = new ConfidenceScore(0.0);

        Assert.Equal(0.0, score.Value);
        Assert.True(score.IsLowConfidence);
    }

    [Fact]
    public void ConfidenceScore_OneBoundary_CreatesSuccessfully()
    {
        var score = new ConfidenceScore(1.0);

        Assert.Equal(1.0, score.Value);
        Assert.False(score.IsLowConfidence);
    }

    [Fact]
    public void ConfidenceScore_BelowZero_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfidenceScore(-0.1));
    }

    [Fact]
    public void ConfidenceScore_AboveOne_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfidenceScore(1.1));
    }

    [Fact]
    public void ConfidenceScore_BelowThreshold_IsLowConfidence()
    {
        var score = new ConfidenceScore(0.69);

        Assert.True(score.IsLowConfidence);
    }

    [Fact]
    public void ConfidenceScore_AtThreshold_IsNotLowConfidence()
    {
        var score = new ConfidenceScore(0.7);

        Assert.False(score.IsLowConfidence);
    }

    [Fact]
    public void ConfidenceScore_AboveThreshold_IsNotLowConfidence()
    {
        var score = new ConfidenceScore(0.95);

        Assert.False(score.IsLowConfidence);
    }

    [Fact]
    public void ConfidenceScore_ImplicitConversion_ReturnsDouble()
    {
        var score = new ConfidenceScore(0.75);

        double value = score;

        Assert.Equal(0.75, value);
    }

    [Fact]
    public void ConfidenceScore_RecordEquality_WorksCorrectly()
    {
        var score1 = new ConfidenceScore(0.85);
        var score2 = new ConfidenceScore(0.85);

        Assert.Equal(score1, score2);
    }

    [Fact]
    public void ConfidenceScore_Threshold_ConstantIs07()
    {
        Assert.Equal(0.7, ConfidenceScore.LowConfidenceThreshold);
    }

    // Test entity for verifying BaseEntity behavior
    private sealed class TestEntity : BaseEntity
    {
    }
}
