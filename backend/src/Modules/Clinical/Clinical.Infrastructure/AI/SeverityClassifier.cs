using Clinical.Domain.Enums;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Classifies conflict severity based on data category (FR-026, AIR-006).
/// Critical: Medications, Allergies (patient safety impact).
/// Warning: All other categories.
/// </summary>
public sealed class SeverityClassifier
{
    /// <summary>
    /// Categories that result in Critical severity conflicts.
    /// These have direct patient safety implications.
    /// </summary>
    private static readonly HashSet<DataCategory> CriticalCategories = new()
    {
        DataCategory.Medication,
        DataCategory.Allergy
    };

    /// <summary>
    /// Classifies the severity of a conflict based on its data category.
    /// </summary>
    /// <param name="category">The data category of the conflicting entries.</param>
    /// <returns>Critical for medications/allergies, Warning for all others.</returns>
    public ConflictSeverity Classify(DataCategory category)
    {
        return CriticalCategories.Contains(category)
            ? ConflictSeverity.Critical
            : ConflictSeverity.Warning;
    }

    /// <summary>
    /// Determines if a category should trigger Critical severity.
    /// </summary>
    /// <param name="category">The data category to check.</param>
    /// <returns>True if the category warrants Critical severity.</returns>
    public bool IsCriticalCategory(DataCategory category)
    {
        return CriticalCategories.Contains(category);
    }
}
