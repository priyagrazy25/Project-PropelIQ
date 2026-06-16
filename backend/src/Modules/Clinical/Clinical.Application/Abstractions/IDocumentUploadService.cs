using Clinical.Application.DTOs;
using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Document upload service handling storage and entity creation (AC-2, AC-5).
/// </summary>
public interface IDocumentUploadService
{
    /// <summary>
    /// Uploads a document, stores it encrypted, and creates the database record.
    /// </summary>
    /// <param name="patientId">Patient owning the document.</param>
    /// <param name="userId">User performing the upload.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="fileSizeBytes">File size in bytes.</param>
    /// <param name="stream">File content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload response or error.</returns>
    Task<Result<DocumentUploadResponse>> UploadDocumentAsync(
        Guid patientId,
        Guid? userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        Stream stream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the processing status of a document.
    /// </summary>
    /// <param name="documentId">Document ID.</param>
    /// <param name="patientId">Patient ID for ownership verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document status or error.</returns>
    Task<Result<DocumentUploadResponse>> GetDocumentStatusAsync(
        Guid documentId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the processing pipeline status for all documents belonging to a patient (SCR-015).
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document pipeline statuses.</returns>
    Task<Result<IReadOnlyList<DocumentPipelineStatusResponse>>> GetAllDocumentsStatusAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
