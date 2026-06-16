using Clinical.Application.DTOs;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Computes per-field and overall confidence scores for AI-extracted intake data (AIR-Q02).
/// Confidence scoring considers field completeness, value specificity, and category coverage.
/// </summary>
public sealed class ConfidenceScorer
{
    private const double LowConfidenceThreshold = 0.5;

    private static readonly HashSet<string> RequiredCategories =
        ["MedicalHistory", "Symptoms", "Allergies", "Medications"];

    /// <summary>
    /// Computes an overall confidence score across all parsed fields.
    /// Returns 0.0 if no fields are present.
    /// </summary>
    public double ComputeOverallConfidence(IReadOnlyList<ParsedFieldDto> fields)
    {
        if (fields.Count == 0)
        {
            return 0.0;
        }

        return fields.Average(f => f.ConfidenceScore);
    }

    /// <summary>
    /// Determines whether the latest extraction round has low confidence,
    /// defined as all new fields scoring below the threshold.
    /// </summary>
    public bool IsLowConfidence(IReadOnlyList<ParsedFieldDto> newFields)
    {
        if (newFields.Count == 0)
        {
            return false;
        }

        return newFields.All(f => f.ConfidenceScore < LowConfidenceThreshold);
    }

    /// <summary>
    /// Evaluates category coverage: how many required categories have at least one
    /// field with confidence >= threshold.
    /// </summary>
    public CategoryCoverageResult EvaluateCoverage(IReadOnlyList<ParsedFieldDto> allFields)
    {
        var coveredCategories = allFields
            .Where(f => f.ConfidenceScore >= LowConfidenceThreshold)
            .Select(f => f.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(c => RequiredCategories.Contains(c));

        return new CategoryCoverageResult(
            CoveredCount: coveredCategories,
            TotalRequired: RequiredCategories.Count,
            CoverageRatio: (double)coveredCategories / RequiredCategories.Count);
    }
}

/// <summary>
/// Result of category coverage evaluation.
/// </summary>
public sealed record CategoryCoverageResult(
    int CoveredCount,
    int TotalRequired,
    double CoverageRatio);
