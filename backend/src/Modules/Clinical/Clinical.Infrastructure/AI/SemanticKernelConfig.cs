using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Clinical.Application.AI;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Semantic Kernel configuration with Ollama's OpenAI-compatible API (AD-004, TR-007).
/// </summary>
public static class SemanticKernelConfig
{
    public static Kernel CreateKernel(OllamaOptions options, ILoggerFactory loggerFactory)
    {
        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);

        // Ollama exposes an OpenAI-compatible API at /v1
        var endpoint = options.Endpoint.TrimEnd('/');
        if (!endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint += "/v1";
        }

#pragma warning disable SKEXP0010 // OpenAI-compatible chat completion with custom endpoint
        builder.AddOpenAIChatCompletion(
            modelId: options.ModelId,
            endpoint: new Uri(endpoint),
            apiKey: "ollama");
#pragma warning restore SKEXP0010

        return builder.Build();
    }
}
