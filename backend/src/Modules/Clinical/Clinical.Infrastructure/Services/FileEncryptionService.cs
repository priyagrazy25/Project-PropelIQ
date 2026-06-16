using System.Security.Cryptography;
using Clinical.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Configuration options for file encryption keys.
/// </summary>
public sealed class FileEncryptionOptions
{
    public const string SectionName = "FileEncryption";

    /// <summary>
    /// Current key ID for new encryptions (supports key rotation).
    /// </summary>
    public string CurrentKeyId { get; set; } = "default";

    /// <summary>
    /// Dictionary of key ID to Base64-encoded 256-bit keys.
    /// </summary>
    public Dictionary<string, string> Keys { get; set; } = new();
}

/// <summary>
/// AES-256-GCM file encryption service for clinical document storage (DR-004, NFR-005).
/// 
/// Format: [12-byte nonce][16-byte auth tag][ciphertext]
/// 
/// AES-GCM provides authenticated encryption with associated data (AEAD),
/// ensuring both confidentiality and integrity of encrypted content.
/// </summary>
public sealed class FileEncryptionService : IFileEncryptionService
{
    private const int NonceSize = 12; // 96-bit nonce for GCM
    private const int TagSize = 16; // 128-bit authentication tag
    private const int KeySize = 32; // 256-bit key

    private readonly FileEncryptionOptions _options;
    private readonly ILogger<FileEncryptionService> _logger;
    private readonly Dictionary<string, byte[]> _keyCache = new();

    public FileEncryptionService(
        IConfiguration configuration,
        ILogger<FileEncryptionService> logger)
    {
        _options = new FileEncryptionOptions();
        configuration.GetSection(FileEncryptionOptions.SectionName).Bind(_options);
        _logger = logger;

        // Pre-load and validate keys
        LoadKeys();
    }

    public async Task<Result<Stream>> EncryptAsync(
        Stream plaintext,
        string keyId,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetKey(keyId, out var key))
            {
                return Result<Stream>.Failure($"Encryption key '{keyId}' not found.");
            }

            // Read all plaintext into memory (required for GCM)
            using var plaintextMs = new MemoryStream();
            await plaintext.CopyToAsync(plaintextMs, cancellationToken);
            var plaintextBytes = plaintextMs.ToArray();

            // Generate cryptographically random nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Prepare output buffers
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            // Encrypt with AES-GCM
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(
                nonce: nonce,
                plaintext: plaintextBytes,
                ciphertext: ciphertext,
                tag: tag,
                associatedData: associatedData);

            // Write encrypted output: [nonce][tag][ciphertext]
            var outputMs = new MemoryStream();
            await outputMs.WriteAsync(nonce, cancellationToken);
            await outputMs.WriteAsync(tag, cancellationToken);
            await outputMs.WriteAsync(ciphertext, cancellationToken);
            outputMs.Position = 0;

            _logger.LogDebug(
                "Encrypted {Size} bytes using key {KeyId}",
                plaintextBytes.Length, keyId);

            return Result<Stream>.Success(outputMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed for key {KeyId}", keyId);
            return Result<Stream>.Failure("Encryption failed. Please try again.");
        }
    }

    public async Task<Result<Stream>> DecryptAsync(
        Stream ciphertext,
        string keyId,
        byte[]? associatedData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetKey(keyId, out var key))
            {
                return Result<Stream>.Failure($"Decryption key '{keyId}' not found.");
            }

            // Read all encrypted data
            using var ciphertextMs = new MemoryStream();
            await ciphertext.CopyToAsync(ciphertextMs, cancellationToken);
            var encryptedBytes = ciphertextMs.ToArray();

            if (encryptedBytes.Length < NonceSize + TagSize)
            {
                return Result<Stream>.Failure("Invalid encrypted data format.");
            }

            // Extract components: [nonce][tag][ciphertext]
            var nonce = encryptedBytes[..NonceSize];
            var tag = encryptedBytes[NonceSize..(NonceSize + TagSize)];
            var ciphertextData = encryptedBytes[(NonceSize + TagSize)..];

            // Prepare output buffer
            var plaintextBytes = new byte[ciphertextData.Length];

            // Decrypt with AES-GCM (validates authentication tag)
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(
                nonce: nonce,
                ciphertext: ciphertextData,
                tag: tag,
                plaintext: plaintextBytes,
                associatedData: associatedData);

            var outputMs = new MemoryStream(plaintextBytes);
            outputMs.Position = 0;

            _logger.LogDebug(
                "Decrypted {Size} bytes using key {KeyId}",
                plaintextBytes.Length, keyId);

            return Result<Stream>.Success(outputMs);
        }
        catch (AuthenticationTagMismatchException)
        {
            _logger.LogWarning("Authentication tag mismatch for key {KeyId} - data may be tampered", keyId);
            return Result<Stream>.Failure("Data integrity verification failed. The file may be corrupted or tampered.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed for key {KeyId}", keyId);
            return Result<Stream>.Failure("Decryption failed. Please try again.");
        }
    }

    public bool IsKeyAvailable(string keyId)
    {
        return _keyCache.ContainsKey(keyId) || 
               _options.Keys.ContainsKey(keyId);
    }

    public string GetCurrentKeyId()
    {
        return _options.CurrentKeyId;
    }

    private void LoadKeys()
    {
        foreach (var (keyId, base64Key) in _options.Keys)
        {
            try
            {
                var key = Convert.FromBase64String(base64Key);
                if (key.Length != KeySize)
                {
                    _logger.LogWarning(
                        "Invalid key size for '{KeyId}': expected {Expected} bytes, got {Actual}",
                        keyId, KeySize, key.Length);
                    continue;
                }
                _keyCache[keyId] = key;
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid Base64 encoding for key '{KeyId}'", keyId);
            }
        }

        // If no keys configured, create a development fallback (NOT for production)
        if (_keyCache.Count == 0)
        {
            _logger.LogWarning(
                "No encryption keys configured. Using development fallback. " +
                "Configure FileEncryption:Keys for production.");
            _keyCache["default"] = SHA256.HashData("UnifiedPatientAccessFileEncryptionKey"u8.ToArray());
        }

        _logger.LogInformation("Loaded {Count} encryption keys", _keyCache.Count);
    }

    private bool TryGetKey(string keyId, out byte[] key)
    {
        if (_keyCache.TryGetValue(keyId, out key!))
        {
            return true;
        }

        // Try to load from configuration if not cached
        if (_options.Keys.TryGetValue(keyId, out var base64Key))
        {
            try
            {
                key = Convert.FromBase64String(base64Key);
                if (key.Length == KeySize)
                {
                    _keyCache[keyId] = key;
                    return true;
                }
            }
            catch (FormatException)
            {
                // Invalid key format
            }
        }

        key = [];
        return false;
    }
}
