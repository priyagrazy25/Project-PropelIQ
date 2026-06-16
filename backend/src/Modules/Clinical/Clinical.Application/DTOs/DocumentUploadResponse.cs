using Clinical.Domain.Enums;

namespace Clinical.Application.DTOs;

/// <summary>
/// Response DTO for document upload endpoint (AC-2, AC-5).
/// </summary>
public sealed record DocumentUploadResponse(
    Guid DocumentId,
    string FileName,
    string ProcessingStatus);
