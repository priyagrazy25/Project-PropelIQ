using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace SharedKernel.Logging;

/// <summary>
/// Middleware that extracts or generates a correlation ID for distributed request tracing (AC-5).
/// Reads from X-Correlation-ID header; if absent, generates a new GUID.
/// Pushes CorrelationId into Serilog LogContext for structured log enrichment.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues value)
            && !StringValues.IsNullOrEmpty(value)
                ? value.ToString()
                : Guid.NewGuid().ToString("D");

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
