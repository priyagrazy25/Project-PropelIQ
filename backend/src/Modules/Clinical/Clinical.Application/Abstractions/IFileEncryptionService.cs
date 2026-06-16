using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// File encryption service using AES-256-GCM for clinical document storage (DR-004, NFR-005).
/// Provides authenticated encryption with associated data (AEAD) for integrity verification.
/// </summary>
public interface IFileEncryptionService
{
    /// <summary>
    /// Encrypts a stream using AES-256-GCM and returns the encrypted content.
    /// </summary>
    /// <param name="plaintext">The plaintext stream to encrypt.</param>
    /// <param name="keyId">Key identifier for key rotation support.</param>
    /// <param name="associatedData">Optional associated data for authentication (e.g., patient ID, document ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Encrypted stream containing nonce, tag, and ciphertext.</returns>
    Task<Result<Stream>> EncryptAsync(
        Stream plaintext,
        string keyId,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a stream that was encrypted using AES-256-GCM.
    /// </summary>
    /// <param name="ciphertext">The encrypted stream to decrypt.</param>
    /// <param name="keyId">Key identifier used for encryption.</param>
    /// <param name="associatedData">Optional associated data for authentication verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decrypted plaintext stream.</returns>
    Task<Result<Stream>> DecryptAsync(
        Stream ciphertext,
        string keyId,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a key exists and is available for the given key ID.
    /// </summary>
    /// <param name="keyId">Key identifier to validate.</param>
    /// <returns>True if the key is available.</returns>
    bool IsKeyAvailable(string keyId);

    /// <summary>
    /// Gets the current default key ID for new encryptions.
    /// </summary>
    string GetCurrentKeyId();
}
