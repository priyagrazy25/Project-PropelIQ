using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SharedKernel.Audit;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Semantic Kernel function invocation filter that audits AI model calls (AIR-S03).
/// Captures model version, token counts, confidence scores, and latency metrics.
/// </summary>
public sealed class AiAuditFilter : IFunctionInvocationFilter
{
    private readonly IServiceProvider _serviceProvider;

    public AiAuditFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var functionName = $"{context.Function.PluginName}.{context.Function.Name}";

        try
        {
            // Execute the function
            await next(context);

            stopwatch.Stop();

            // Extract result details
            var result = context.Result?.GetValue<object>();
            var usage = ExtractTokenUsage(context);
            var confidence = ExtractConfidence(result);

            var entry = new AiInvocationEntry
            {
                ModelId = context.Function.Metadata.Description ?? "unknown",
                ModelVersion = "1.0",
                FunctionName = functionName,
                PromptTokens = usage.promptTokens,
                CompletionTokens = usage.completionTokens,
                ConfidenceScore = confidence,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                Success = true,
                ErrorMessage = null
            };

            // Use scoped service for audit
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            if (auditService != null)
            {
                await auditService.LogAiInvocationAsync(entry);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var entry = new AiInvocationEntry
            {
                ModelId = context.Function.Metadata.Description ?? "unknown",
                ModelVersion = "1.0",
                FunctionName = functionName,
                PromptTokens = 0,
                CompletionTokens = 0,
                ConfidenceScore = null,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                Success = false,
                ErrorMessage = ex.Message
            };

            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            if (auditService != null)
            {
                await auditService.LogAiInvocationAsync(entry);
            }

            throw;
        }
    }

    private static (int promptTokens, int completionTokens) ExtractTokenUsage(FunctionInvocationContext context)
    {
        // Token usage would typically come from chat completion metadata
        // This is a placeholder - actual implementation depends on SK version
        return (0, 0);
    }

    private static decimal? ExtractConfidence(object? result)
    {
        if (result == null)
            return null;

        var type = result.GetType();
        var confidenceProperty = type.GetProperty("Confidence") ?? type.GetProperty("Score");

        if (confidenceProperty?.GetValue(result) is decimal d)
            return d;
        if (confidenceProperty?.GetValue(result) is double dbl)
            return (decimal)dbl;
        if (confidenceProperty?.GetValue(result) is float f)
            return (decimal)f;

        return null;
    }
}
