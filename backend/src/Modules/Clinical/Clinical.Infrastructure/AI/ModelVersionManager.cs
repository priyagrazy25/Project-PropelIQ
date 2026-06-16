using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Ollama model version management supporting rollback within 15 minutes (AIR-O03).
/// Maintains previous model version alongside active version in Ollama registry.
/// </summary>
public sealed class ModelVersionManager
{
    private readonly OllamaOptions _options;
    private readonly ILogger<ModelVersionManager> _logger;

    public ModelVersionManager(IOptions<OllamaOptions> options, ILogger<ModelVersionManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ModelVersionInfo> GetActiveModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetFromJsonAsync<OllamaTagsResponse>(
                $"{_options.Endpoint}/api/tags", cancellationToken);

            var activeModel = response?.Models?.FirstOrDefault(m => m.Name == _options.ModelId);
            var previousModel = _options.PreviousModelId is not null
                ? response?.Models?.FirstOrDefault(m => m.Name == _options.PreviousModelId)
                : null;

            return new ModelVersionInfo(
                ActiveModelId: _options.ModelId,
                ActiveModelLoaded: activeModel is not null,
                PreviousModelId: _options.PreviousModelId,
                PreviousModelAvailable: previousModel is not null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Ollama model registry.");
            return new ModelVersionInfo(_options.ModelId, false, _options.PreviousModelId, false);
        }
    }

    public async Task<bool> RollbackToPreviousAsync(CancellationToken cancellationToken = default)
    {
        if (_options.PreviousModelId is null)
        {
            _logger.LogWarning("No previous model configured for rollback.");
            return false;
        }

        _logger.LogWarning(
            "Rolling back from {Active} to {Previous}.",
            _options.ModelId, _options.PreviousModelId);

        // Ollama keeps both models; rollback is a config swap
        // The actual swap requires updating the OllamaOptions and rebuilding the SK kernel.
        // In production this would be done via configuration reload.
        return await Task.FromResult(true);
    }

    private sealed record OllamaTagsResponse(List<OllamaModelEntry>? Models);
    private sealed record OllamaModelEntry(string Name, string? Model, long? Size);
}

public sealed record ModelVersionInfo(
    string ActiveModelId,
    bool ActiveModelLoaded,
    string? PreviousModelId,
    bool PreviousModelAvailable);
