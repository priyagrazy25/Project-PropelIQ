using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace SharedKernel.Validation;

/// <summary>
/// Startup validator that enforces AIR-S01 compliance by ensuring Ollama AI runtime
/// is configured for local-only execution. Prevents PHI transmission to external AI providers.
/// </summary>
public sealed class AiNetworkGuard : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiNetworkGuard> _logger;

    // Allowed local addresses for Ollama endpoint
    private static readonly HashSet<string> AllowedLocalHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "[::1]"
    };

    public AiNetworkGuard(IConfiguration configuration, ILogger<AiNetworkGuard> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var ollamaEndpoint = _configuration["Ollama:Endpoint"];

        // If Ollama is not configured, skip validation (AI features disabled)
        if (string.IsNullOrWhiteSpace(ollamaEndpoint))
        {
            _logger.LogInformation("Ollama endpoint not configured; AI features disabled.");
            return Task.CompletedTask;
        }

        if (!Uri.TryCreate(ollamaEndpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"AIR-S01 violation: Invalid Ollama endpoint URL format: '{ollamaEndpoint}'. " +
                "Ollama must be configured with a valid localhost URL.");
        }

        var host = uri.Host;

        // Direct hostname check for common local addresses
        if (AllowedLocalHosts.Contains(host))
        {
            _logger.LogInformation(
                "AIR-S01 compliance verified: Ollama endpoint '{Endpoint}' resolves to localhost.",
                ollamaEndpoint);
            return Task.CompletedTask;
        }

        // DNS resolution check for custom hostnames
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            var isLocal = addresses.Any(addr =>
                IPAddress.IsLoopback(addr) ||
                addr.Equals(IPAddress.Parse("127.0.0.1")) ||
                addr.Equals(IPAddress.IPv6Loopback));

            if (isLocal)
            {
                _logger.LogInformation(
                    "AIR-S01 compliance verified: Ollama endpoint '{Endpoint}' resolves to local address.",
                    ollamaEndpoint);
                return Task.CompletedTask;
            }

            // External address detected - fail startup
            var resolvedAddresses = string.Join(", ", addresses.Select(a => a.ToString()));
            throw new InvalidOperationException(
                $"AIR-S01 violation: Ollama must run locally to prevent PHI transmission. " +
                $"Configured endpoint '{ollamaEndpoint}' resolves to external address(es): {resolvedAddresses}. " +
                "Change 'Ollama:Endpoint' to 'http://localhost:11434' or equivalent local address.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // DNS resolution failed - warn but allow startup (Ollama may not be running)
            _logger.LogWarning(
                ex,
                "Could not verify Ollama endpoint locality for '{Endpoint}'. " +
                "Ensure Ollama is running on localhost before using AI features.",
                ollamaEndpoint);
            return Task.CompletedTask;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Extension methods for registering AiNetworkGuard.
/// </summary>
public static class AiNetworkGuardExtensions
{
    /// <summary>
    /// Registers the AI network guard as a hosted service to validate Ollama locality at startup.
    /// </summary>
    public static IServiceCollection AddAiNetworkGuard(this IServiceCollection services)
    {
        services.AddHostedService<AiNetworkGuard>();
        return services;
    }
}
