using System.Security.Cryptography;
using Clinical.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Configuration options for document storage (DR-004).
/// </summary>
public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>
    /// Base path for document storage.
    /// </summary>
    public string BasePath { get; set; } = "./documents";

    /// <summary>
    /// 256-bit encryption key (Base64 encoded).
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Maximum file size in bytes (default 25 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;

    /// <summary>
    /// Allowed content types.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = ["application/pdf"];
}

/// <summary>
/// Document storage service with AES-256 encryption at rest (DR-004).
/// Files are stored with encrypted content and patient-partitioned paths.
/// </summary>
public sealed class DocumentStorageService : IDocumentStorageService
{
    private readonly DocumentStorageOptions _options;
    private readonly ILogger<DocumentStorageService> _logger;

    public DocumentStorageService(
        IOptions<DocumentStorageOptions> options,
        ILogger<DocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Ensure base directory exists
        if (!Directory.Exists(_options.BasePath))
        {
            Directory.CreateDirectory(_options.BasePath);
        }
    }

    public async Task<Result<string>> StoreDocumentAsync(
        Guid patientId,
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique encrypted file path
            var documentId = Guid.NewGuid();
            var patientFolder = Path.Combine(_options.BasePath, patientId.ToString("N"));

            if (!Directory.Exists(patientFolder))
            {
                Directory.CreateDirectory(patientFolder);
            }

            var encryptedFileName = $"{documentId:N}.enc";
            var encryptedFilePath = Path.Combine(patientFolder, encryptedFileName);

            // Encrypt and store
            await using var fileStream = new FileStream(
                encryptedFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await EncryptStreamAsync(stream, fileStream, cancellationToken);

            _logger.LogInformation(
                "Document stored: {DocumentId} for patient {PatientId}",
                documentId, patientId);

            // Return relative path for database storage
            var relativePath = Path.Combine(patientId.ToString("N"), encryptedFileName);
            return Result<string>.Success(relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store document for patient {PatientId}", patientId);
            return Result<string>.Failure("Failed to store document. Please try again.");
        }
    }

    public async Task<Result<Stream>> GetDocumentAsync(
        string encryptedFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_options.BasePath, encryptedFilePath);

            if (!File.Exists(fullPath))
            {
                return Result<Stream>.Failure("Document not found.");
            }

            await using var encryptedStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            var decryptedStream = new MemoryStream();
            await DecryptStreamAsync(encryptedStream, decryptedStream, cancellationToken);
            decryptedStream.Position = 0;

            return Result<Stream>.Success(decryptedStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document: {Path}", encryptedFilePath);
            return Result<Stream>.Failure("Failed to retrieve document.");
        }
    }

    public Task<Result<bool>> DeleteDocumentAsync(
        string encryptedFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_options.BasePath, encryptedFilePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Document deleted: {Path}", encryptedFilePath);
            }

            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document: {Path}", encryptedFilePath);
            return Task.FromResult(Result<bool>.Failure("Failed to delete document."));
        }
    }

    private async Task EncryptStreamAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        using var aes = Aes.Create();
        aes.Key = GetEncryptionKey();
        aes.GenerateIV();

        // Write IV to output first (needed for decryption)
        await output.WriteAsync(aes.IV, cancellationToken);

        await using var cryptoStream = new CryptoStream(
            output,
            aes.CreateEncryptor(),
            CryptoStreamMode.Write,
            leaveOpen: true);

        await input.CopyToAsync(cryptoStream, cancellationToken);
        await cryptoStream.FlushFinalBlockAsync(cancellationToken);
    }

    private async Task DecryptStreamAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        using var aes = Aes.Create();
        aes.Key = GetEncryptionKey();

        // Read IV from input
        var iv = new byte[aes.BlockSize / 8];
        var bytesRead = await input.ReadAsync(iv.AsMemory(0, iv.Length), cancellationToken);
        if (bytesRead != iv.Length)
        {
            throw new InvalidDataException("Invalid encrypted file format.");
        }
        aes.IV = iv;

        await using var cryptoStream = new CryptoStream(
            input,
            aes.CreateDecryptor(),
            CryptoStreamMode.Read,
            leaveOpen: true);

        await cryptoStream.CopyToAsync(output, cancellationToken);
    }

    private byte[] GetEncryptionKey()
    {
        if (string.IsNullOrEmpty(_options.EncryptionKey))
        {
            // Development fallback - use deterministic key (NOT for production)
            _logger.LogWarning("Using default encryption key. Configure DocumentStorage:EncryptionKey for production.");
            return SHA256.HashData("UnifiedPatientAccessDocumentEncryptionKey"u8.ToArray());
        }

        return Convert.FromBase64String(_options.EncryptionKey);
    }
}
