using Clinical.Domain.Enums;
using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class DataConflict : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public Guid? ConflictingDocumentId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string SourceValue { get; set; } = string.Empty;
    public string ConflictingValue { get; set; } = string.Empty;
    public ConflictSeverity Severity { get; set; } = ConflictSeverity.Warning;
    public ConflictResolutionStatus ResolutionStatus { get; set; } = ConflictResolutionStatus.Open;
    public Guid? ResolvedByUserId { get; set; }
    public string? StaffNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public ClinicalDocument? SourceDocument { get; set; }
    public ClinicalDocument? ConflictingDocument { get; set; }
}
