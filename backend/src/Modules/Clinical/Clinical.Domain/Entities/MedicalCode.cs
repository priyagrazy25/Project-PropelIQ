using Clinical.Domain.Enums;
using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class MedicalCode : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? ExtractedDataId { get; set; }
    public MedicalCodeType CodeType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Override fields (AC-5: preserve AI suggestion alongside manual override)
    public bool IsOverridden { get; set; }
    public string? OriginalAiCode { get; set; }
    public string? OriginalAiDescription { get; set; }
    public double? OriginalAiConfidence { get; set; }
    public string? OverrideReason { get; set; }
    public string? OverrideNotes { get; set; }

    // Navigation
    public ClinicalDocument? Document { get; set; }
    public ExtractedData? ExtractedData { get; set; }
}
