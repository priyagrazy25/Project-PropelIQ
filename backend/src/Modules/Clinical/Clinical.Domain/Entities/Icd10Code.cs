using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

/// <summary>
/// ICD-10-CM reference lookup table for medical coding (AIR-004).
/// Contains common diagnosis codes for mapping against NER-classified entities.
/// </summary>
public sealed class Icd10Code : BaseEntity
{
    /// <summary>
    /// ICD-10-CM code (e.g., "E11.9", "I10").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full description of the diagnosis.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Short description for quick reference.
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Category chapter (e.g., "Endocrine", "Circulatory").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Keywords for fuzzy matching during NER classification.
    /// Pipe-delimited (e.g., "diabetes|diabetic|hyperglycemia").
    /// </summary>
    public string Keywords { get; set; } = string.Empty;

    /// <summary>
    /// Whether this code is billable (leaf node in ICD-10 hierarchy).
    /// </summary>
    public bool IsBillable { get; set; } = true;
}
