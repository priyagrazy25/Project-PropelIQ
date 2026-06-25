using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Scheduling.Application.Abstractions;

namespace Scheduling.Infrastructure.ML;

/// <summary>
/// Internal storage record for model version with additional metadata.
/// </summary>
public sealed record StoredModelVersion(
    string Version,
    DateTime TrainedAt,
    double Accuracy,
    double AUC,
    double F1Score,
    int TrainingSamples,
    string ModelPath,
    bool IsActive);

/// <summary>
/// Manages model versions with rollback capability (AIR-O03).
/// Supports rollback within 15 minutes of deployment.
/// </summary>
public sealed class ModelVersionManager
{
    private const int MaxVersionsToKeep = 10;
    private static readonly TimeSpan RollbackWindow = TimeSpan.FromMinutes(15);

    private readonly string _modelDirectory;
    private readonly string _metadataPath;
    private readonly ILogger<ModelVersionManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ModelVersionMetadata _metadata;

    public ModelVersionManager(string modelDirectory, ILogger<ModelVersionManager> logger)
    {
        _modelDirectory = modelDirectory;
        _metadataPath = Path.Combine(modelDirectory, "version-metadata.json");
        _logger = logger;

        EnsureDirectoryExists();
        _metadata = LoadMetadata();
    }

    /// <summary>
    /// Saves a new model version and returns version info.
    /// </summary>
    public async Task<StoredModelVersion> SaveVersionAsync(
        ITransformer model,
        DataViewSchema schema,
        ModelMetrics metrics,
        MLContext mlContext,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var version = GenerateVersion();
            var modelPath = GetModelPath(version);

            // Save model file
            mlContext.Model.Save(model, schema, modelPath);

            // Create version info
            var versionInfo = new StoredModelVersion(
                Version: version,
                TrainedAt: DateTime.UtcNow,
                Accuracy: metrics.Accuracy,
                AUC: metrics.AreaUnderCurve,
                F1Score: metrics.F1Score,
                TrainingSamples: metrics.TrainingSamples,
                ModelPath: modelPath,
                IsActive: false);

            // Add to metadata
            _metadata.Versions.Add(versionInfo);
            await SaveMetadataAsync(cancellationToken);

            _logger.LogInformation(
                "Saved model version {Version} with Accuracy={Accuracy:P2}",
                version, metrics.Accuracy);

            // Cleanup old versions
            await CleanupOldVersionsAsync(cancellationToken);

            return versionInfo;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Activates a model version for serving.
    /// </summary>
    public async Task<bool> ActivateVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var versionInfo = _metadata.Versions.FirstOrDefault(v => v.Version == version);
            if (versionInfo == null)
            {
                _logger.LogWarning("Version {Version} not found", version);
                return false;
            }

            // Store previous active version for potential rollback
            var previousActive = _metadata.Versions.FirstOrDefault(v => v.IsActive);
            if (previousActive != null)
            {
                _metadata.PreviousActiveVersion = previousActive.Version;
                _metadata.ActivationTimestamp = DateTime.UtcNow;

                // Mark previous as inactive
                var idx = _metadata.Versions.IndexOf(previousActive);
                _metadata.Versions[idx] = previousActive with { IsActive = false };
            }

            // Activate new version
            var newIdx = _metadata.Versions.IndexOf(versionInfo);
            _metadata.Versions[newIdx] = versionInfo with { IsActive = true };
            _metadata.CurrentVersion = version;

            await SaveMetadataAsync(cancellationToken);

            _logger.LogInformation("Activated model version {Version}", version);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Rolls back to a previous model version.
    /// Returns true if rollback successful, false if outside window or version not found.
    /// </summary>
    public async Task<(bool Success, string? Message)> RollbackAsync(
        string? targetVersion = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // If no target specified, rollback to previous
            targetVersion ??= _metadata.PreviousActiveVersion;

            if (string.IsNullOrEmpty(targetVersion))
            {
                return (false, "No previous version available for rollback");
            }

            // Check rollback window (AIR-O03: ≤15min)
            if (_metadata.ActivationTimestamp.HasValue)
            {
                var elapsed = DateTime.UtcNow - _metadata.ActivationTimestamp.Value;
                if (elapsed > RollbackWindow)
                {
                    return (false, $"Rollback window expired. Last activation was {elapsed.TotalMinutes:N1} minutes ago (limit: 15 min)");
                }
            }

            var targetVersionInfo = _metadata.Versions.FirstOrDefault(v => v.Version == targetVersion);
            if (targetVersionInfo == null)
            {
                return (false, $"Target version {targetVersion} not found");
            }

            if (!File.Exists(targetVersionInfo.ModelPath))
            {
                return (false, $"Model file for version {targetVersion} not found");
            }

            // Deactivate current
            var current = _metadata.Versions.FirstOrDefault(v => v.IsActive);
            if (current != null)
            {
                var idx = _metadata.Versions.IndexOf(current);
                _metadata.Versions[idx] = current with { IsActive = false };
            }

            // Activate target
            var targetIdx = _metadata.Versions.IndexOf(targetVersionInfo);
            _metadata.Versions[targetIdx] = targetVersionInfo with { IsActive = true };
            _metadata.CurrentVersion = targetVersion;

            // Clear rollback state (one rollback per activation)
            _metadata.PreviousActiveVersion = null;
            _metadata.ActivationTimestamp = null;

            await SaveMetadataAsync(cancellationToken);

            _logger.LogInformation("Rolled back to model version {Version}", targetVersion);
            return (true, $"Successfully rolled back to version {targetVersion}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets information about all available versions (public contract).
    /// </summary>
    public IReadOnlyList<ModelVersionInfo> GetAllVersions()
    {
        return _metadata.Versions
            .Select(ToPublicVersionInfo)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the currently active model version info (public contract).
    /// </summary>
    public ModelVersionInfo? GetActiveVersion()
    {
        var stored = _metadata.Versions.FirstOrDefault(v => v.IsActive);
        return stored != null ? ToPublicVersionInfo(stored) : null;
    }

    /// <summary>
    /// Gets internal stored version info for the active model.
    /// </summary>
    public StoredModelVersion? GetActiveStoredVersion()
    {
        var activeVersion = _metadata.Versions.FirstOrDefault(v => v.IsActive);
        return activeVersion is null ? null : ResolveStoredVersion(activeVersion);
    }

    /// <summary>
    /// Gets the path to the active model file.
    /// </summary>
    public string? GetActiveModelPath()
    {
        return GetActiveStoredVersion()?.ModelPath;
    }

    /// <summary>
    /// Gets the canonical model path for a version in the current model directory.
    /// </summary>
    public string GetCanonicalModelPath(string version)
    {
        return GetModelPath(version);
    }

    /// <summary>
    /// Checks if rollback is still allowed (within 15-minute window).
    /// </summary>
    public bool CanRollback()
    {
        if (!_metadata.ActivationTimestamp.HasValue || string.IsNullOrEmpty(_metadata.PreviousActiveVersion))
            return false;

        var elapsed = DateTime.UtcNow - _metadata.ActivationTimestamp.Value;
        return elapsed <= RollbackWindow;
    }

    /// <summary>
    /// Gets remaining rollback time in seconds, or 0 if expired.
    /// </summary>
    public int GetRemainingRollbackSeconds()
    {
        if (!_metadata.ActivationTimestamp.HasValue)
            return 0;

        var elapsed = DateTime.UtcNow - _metadata.ActivationTimestamp.Value;
        var remaining = RollbackWindow - elapsed;
        return remaining > TimeSpan.Zero ? (int)remaining.TotalSeconds : 0;
    }

    private string GenerateVersion()
    {
        return $"v{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    private string GetModelPath(string version)
    {
        return Path.Combine(_modelDirectory, $"noshow-model-{version}.zip");
    }

    private StoredModelVersion ResolveStoredVersion(StoredModelVersion storedVersion)
    {
        if (File.Exists(storedVersion.ModelPath))
        {
            return storedVersion;
        }

        var canonicalPath = GetModelPath(storedVersion.Version);
        if (string.Equals(storedVersion.ModelPath, canonicalPath, StringComparison.OrdinalIgnoreCase))
        {
            return storedVersion;
        }

        if (File.Exists(canonicalPath))
        {
            _logger.LogInformation(
                "Resolved model version {Version} to current workspace path {Path}",
                storedVersion.Version,
                canonicalPath);

            return storedVersion with { ModelPath = canonicalPath };
        }

        return storedVersion with { ModelPath = canonicalPath };
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_modelDirectory))
        {
            Directory.CreateDirectory(_modelDirectory);
            _logger.LogInformation("Created model directory: {Path}", _modelDirectory);
        }
    }

    private ModelVersionMetadata LoadMetadata()
    {
        if (!File.Exists(_metadataPath))
        {
            return new ModelVersionMetadata();
        }

        try
        {
            var json = File.ReadAllText(_metadataPath);
            return JsonSerializer.Deserialize<ModelVersionMetadata>(json) ?? new ModelVersionMetadata();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load metadata, starting fresh");
            return new ModelVersionMetadata();
        }
    }

    private async Task SaveMetadataAsync(CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(_metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_metadataPath, json, cancellationToken);
    }

    private async Task CleanupOldVersionsAsync(CancellationToken cancellationToken)
    {
        var inactiveVersions = _metadata.Versions
            .Where(v => !v.IsActive)
            .OrderByDescending(v => v.TrainedAt)
            .Skip(MaxVersionsToKeep - 1) // Keep MaxVersionsToKeep including active
            .ToList();

        foreach (var version in inactiveVersions)
        {
            try
            {
                if (File.Exists(version.ModelPath))
                {
                    File.Delete(version.ModelPath);
                }
                _metadata.Versions.Remove(version);
                _logger.LogInformation("Cleaned up old version {Version}", version.Version);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup version {Version}", version.Version);
            }
        }

        if (inactiveVersions.Count > 0)
        {
            await SaveMetadataAsync(cancellationToken);
        }
    }

    private static ModelVersionInfo ToPublicVersionInfo(StoredModelVersion stored)
    {
        return new ModelVersionInfo(
            Version: stored.Version,
            TrainedAt: stored.TrainedAt,
            TrainingSamples: stored.TrainingSamples,
            Accuracy: stored.Accuracy,
            IsActive: stored.IsActive);
    }
}

/// <summary>
/// Metadata for version management persistence.
/// </summary>
internal sealed class ModelVersionMetadata
{
    public string? CurrentVersion { get; set; }
    public string? PreviousActiveVersion { get; set; }
    public DateTime? ActivationTimestamp { get; set; }
    public List<StoredModelVersion> Versions { get; set; } = [];
}
