using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Host.HealthChecks;

/// <summary>
/// Health check that verifies Ollama AI runtime connectivity by calling /api/tags.
/// Returns Degraded (not Unhealthy) on timeout per edge case spec.
/// </summary>
public sealed class OllamaHealthCheck : IHealthCheck
{
    private readonly string _endpoint;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public OllamaHealthCheck(IConfiguration configuration)
    {
        _endpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = Timeout };
            var response = await httpClient.GetAsync(
                $"{_endpoint.TrimEnd('/')}/api/tags", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Ollama is reachable at {_endpoint}.")
                : HealthCheckResult.Degraded($"Ollama returned {response.StatusCode}.");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Ollama health check timed out after 5s.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Ollama is unreachable.", ex);
        }
    }
}
