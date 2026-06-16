using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Document storage service for encrypted clinical document persistence (DR-004).
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Stores a document to encrypted storage and returns the encrypted file path.
    /// </summary>
    /// <param name="patientId">Patient owning the document.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="stream">File content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Encrypted file path or error.</returns>
    Task<Result<string>> StoreDocumentAsync(
        Guid patientId,
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a document from encrypted storage.
    /// </summary>
    /// <param name="encryptedFilePath">Encrypted file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File content stream or error.</returns>
    Task<Result<Stream>> GetDocumentAsync(
        string encryptedFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from encrypted storage.
    /// </summary>
    /// <param name="encryptedFilePath">Encrypted file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error.</returns>
    Task<Result<bool>> DeleteDocumentAsync(
        string encryptedFilePath,
        CancellationToken cancellationToken = default);
}
