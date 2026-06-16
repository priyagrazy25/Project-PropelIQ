using System.Security.Claims;
using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedKernel.Audit;

namespace Clinical.API.Controllers;

/// <summary>
/// Document upload endpoints for clinical document processing (UC-008, AC-2, AC-4, AC-5).
/// </summary>
[ApiController]
[Route("api/clinical/documents")]
[Authorize]
[Produces("application/json")]
public class DocumentUploadController : ControllerBase
{
    private readonly IDocumentUploadService _uploadService;
    private readonly IAuditService? _auditService;
    private readonly ILogger<DocumentUploadController> _logger;

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    public DocumentUploadController(
        IDocumentUploadService uploadService,
        ILogger<DocumentUploadController> logger,
        IAuditService? auditService = null)
    {
        _uploadService = uploadService;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Uploads a clinical document for OCR/NLP processing (AC-2, AC-5).
    /// </summary>
    /// <param name="file">PDF file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document ID and processing status.</returns>
    /// <response code="201">Document uploaded and queued.</response>
    /// <response code="400">Invalid file type or size.</response>
    /// <response code="413">File exceeds maximum size.</response>
    [HttpPost("upload")]
    [RequestSizeLimit(26_214_400)] // 25 MB + overhead
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Validate file presence
        if (file is null || file.Length == 0)
        {
            return Problem(
                detail: "No file provided.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        // Validate file size (AC-4 edge case)
        if (file.Length > MaxFileSizeBytes)
        {
            return Problem(
                detail: $"File exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.",
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "File Too Large");
        }

        // Validate content type (AC-4: Non-PDF rejected)
        var contentType = file.ContentType;
        var extension = Path.GetExtension(file.FileName);

        if (!AllowedContentTypes.Contains(contentType) && !AllowedExtensions.Contains(extension))
        {
            return Problem(
                detail: "Only PDF files are accepted.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid File Type");
        }

        // Basic content validation (magic bytes check for PDF)
        if (!await IsPdfContentAsync(file, cancellationToken))
        {
            return Problem(
                detail: "File content does not match PDF format.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid File Content");
        }

        // Get patient ID from claims
        var patientIdClaim = User.FindFirstValue("PatientId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(patientIdClaim, out var patientId))
        {
            return Problem(
                detail: "Unable to identify patient.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Authentication Error");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdClaim, out var userId);

        // Upload document via service (DR-004, AC-2, AC-5)
        await using var stream = file.OpenReadStream();
        var result = await _uploadService.UploadDocumentAsync(
            patientId,
            userId == Guid.Empty ? null : userId,
            file.FileName,
            contentType,
            file.Length,
            stream,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Upload Error");
        }

        // Audit log for document upload (HIPAA compliance)
        if (_auditService != null)
        {
            var userName = User.FindFirstValue("name")
                           ?? User.FindFirstValue(ClaimTypes.Name)
                           ?? "Unknown";

            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = userId == Guid.Empty ? null : userId,
                ActorName = userName,
                Action = "DocumentUpload",
                Resource = "Document",
                ResourceId = result.Value!.DocumentId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { result.Value.DocumentId, result.Value.FileName, patientId, FileSizeBytes = file.Length }
            }, cancellationToken);
        }

        return CreatedAtAction(
            nameof(GetDocumentStatus),
            new { documentId = result.Value!.DocumentId },
            result.Value);
    }

    /// <summary>
    /// Gets the processing pipeline status for all documents belonging to the authenticated patient (SCR-015).
    /// Used by Processing Status page for UXR-403 processing transparency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document pipeline statuses.</returns>
    [HttpGet("processing-status")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentPipelineStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcessingStatus(CancellationToken cancellationToken = default)
    {
        var patientIdClaim = User.FindFirstValue("PatientId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(patientIdClaim, out var patientId))
        {
            return Ok(Array.Empty<DocumentPipelineStatusResponse>());
        }

        var result = await _uploadService.GetAllDocumentsStatusAsync(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Ok(Array.Empty<DocumentPipelineStatusResponse>());
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the processing status of a document.
    /// </summary>
    /// <param name="documentId">Document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document status.</returns>
    [HttpGet("{documentId:guid}/status")]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentStatus(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        // Get patient ID from claims
        var patientIdClaim = User.FindFirstValue("PatientId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(patientIdClaim, out var patientId))
        {
            return NotFound();
        }

        var result = await _uploadService.GetDocumentStatusAsync(
            documentId,
            patientId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound();
        }

        // Audit log for document access (HIPAA compliance)
        if (_auditService != null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue("name")
                           ?? User.FindFirstValue(ClaimTypes.Name)
                           ?? "Unknown";

            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = Guid.TryParse(userId, out var uid) ? uid : null,
                ActorName = userName,
                Action = "DocumentAccess",
                Resource = "Document",
                ResourceId = documentId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { documentId, patientId, AccessType = "ViewStatus" }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Validates PDF content by checking magic bytes.
    /// </summary>
    private static async Task<bool> IsPdfContentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        // PDF magic bytes: %PDF
        var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var buffer = new byte[4];

        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 4), cancellationToken);

        return bytesRead == 4 && buffer.AsSpan().SequenceEqual(pdfSignature);
    }
}
