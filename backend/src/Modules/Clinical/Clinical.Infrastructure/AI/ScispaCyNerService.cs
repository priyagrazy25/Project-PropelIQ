using System.Net.Http.Json;
using Clinical.Application.AI;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// HTTP client for the scispaCy NER Python microservice (AIR-001).
/// Supports basic extraction and clinical extraction with entity mapping.
/// </summary>
public sealed class ScispaCyNerService : INerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScispaCyNerService> _logger;

    public ScispaCyNerService(HttpClient httpClient, ILogger<ScispaCyNerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<NerResult> ExtractEntitiesAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/extract",
                new { text },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<NerApiResponse>(cancellationToken: cancellationToken);

            if (result?.Entities is null)
            {
                return new NerResult([], Success: false, Error: "Empty response from NER service.");
            }

            var entities = result.Entities
                .Select(e => new NerEntity(e.Text, e.Label, e.Start, e.End, e.Confidence))
                .ToList();

            return new NerResult(entities, Success: true);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "scispaCy NER service unavailable.");
            return new NerResult([], Success: false, Error: "NER service unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NER entity extraction failed.");
            return new NerResult([], Success: false, Error: ex.Message);
        }
    }

    public async Task<ClinicalNerResult> ExtractClinicalEntitiesAsync(
        string text,
        string? context = null,
        int? sourcePage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ClinicalExtractRequest(text, context ?? string.Empty, sourcePage);
            var response = await _httpClient.PostAsJsonAsync(
                "/extract/clinical",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ClinicalApiResponse>(
                cancellationToken: cancellationToken);

            if (result is null)
            {
                return new ClinicalNerResult(
                    Entities: [],
                    TotalCount: 0,
                    LowConfidenceCount: 0,
                    SchemaValidityPercent: 0f,
                    Success: false,
                    Error: "Empty response from NER service.");
            }

            var entities = result.Entities
                .Select(e => new ClinicalNerEntity(
                    Text: e.Text,
                    Label: e.Label,
                    Start: e.Start,
                    End: e.End,
                    Confidence: e.Confidence,
                    Category: e.Category,
                    Key: e.Key,
                    Value: e.Value,
                    Unit: e.Unit,
                    IsLowConfidence: e.IsLowConfidence,
                    SourcePage: e.SourcePage))
                .ToList();

            _logger.LogDebug(
                "Clinical NER extraction: {EntityCount} entities, {LowConfCount} low confidence, {ValidityPercent}% validity",
                result.TotalCount, result.LowConfidenceCount, result.SchemaValidityPercent);

            return new ClinicalNerResult(
                Entities: entities,
                TotalCount: result.TotalCount,
                LowConfidenceCount: result.LowConfidenceCount,
                SchemaValidityPercent: result.SchemaValidityPercent,
                Success: true);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "scispaCy NER service unavailable.");
            return new ClinicalNerResult([], 0, 0, 0f, Success: false, Error: "NER service unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clinical NER extraction failed.");
            return new ClinicalNerResult([], 0, 0, 0f, Success: false, Error: ex.Message);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Request/Response DTOs
    private sealed record NerApiResponse(List<NerApiEntity> Entities);
    private sealed record NerApiEntity(string Text, string Label, int Start, int End, float Confidence);

    private sealed record ClinicalExtractRequest(string Text, string Context, int? SourcePage);

    private sealed record ClinicalApiResponse(
        List<ClinicalApiEntity> Entities,
        int TotalCount,
        int LowConfidenceCount,
        float SchemaValidityPercent,
        List<string> ValidationErrors,
        List<string> ValidationWarnings);

    private sealed record ClinicalApiEntity(
        string Text,
        string Label,
        int Start,
        int End,
        float Confidence,
        string Category,
        string Key,
        string Value,
        string? Unit,
        bool IsLowConfidence,
        int? SourcePage);
}

public sealed class NerServiceOptions
{
    public const string SectionName = "NerService";

    public string BaseUrl { get; set; } = "http://localhost:5100";
}
