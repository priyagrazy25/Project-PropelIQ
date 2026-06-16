using Clinical.Domain.Enums;

namespace Clinical.Application.DTOs;

/// <summary>
/// Response DTO for document processing pipeline status (SCR-015).
/// Shows current step, completed steps, and progress for UXR-403 processing transparency.
/// </summary>
public sealed record DocumentPipelineStatusResponse(
    Guid DocumentId,
    string FileName,
    string Status,
    string CurrentStep,
    string[] CompletedSteps,
    int Progress);
