using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

/// <summary>
/// CPT (Current Procedural Terminology) reference lookup table for medical coding (AIR-005).
/// Contains common procedure codes for mapping against NER-classified entities.
/// </summary>
public sealed class CptCode : BaseEntity
{
    /// <summary>
    /// CPT code (e.g., "99213", "36415").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full description of the procedure.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Short description for quick reference.
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping (e.g., "E/M", "Laboratory", "Surgery").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Keywords for fuzzy matching during NER classification.
    /// Pipe-delimited (e.g., "office visit|outpatient|evaluation").
    /// </summary>
    public string Keywords { get; set; } = string.Empty;

    /// <summary>
    /// Relative Value Units for billing reference.
    /// </summary>
    public decimal RVU { get; set; }

    /// <summary>
    /// Whether this code is active (not deprecated).
    /// </summary>
    public bool IsActive { get; set; } = true;
}
