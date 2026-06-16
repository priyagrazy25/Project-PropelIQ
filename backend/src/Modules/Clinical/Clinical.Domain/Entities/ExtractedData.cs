using Clinical.Domain.Enums;
using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class ExtractedData : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid PatientId { get; set; }
    public DataCategory Category { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public int? SourcePage { get; set; }
    public string? SourceText { get; set; }

    // Navigation
    public ClinicalDocument Document { get; set; } = null!;
}
