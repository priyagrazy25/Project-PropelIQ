using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Middleware;

/// <summary>
/// Middleware that adds OWASP-recommended security headers to all HTTP responses (NFR-010).
/// Includes Content-Security-Policy, X-Frame-Options, X-Content-Type-Options, and Referrer-Policy.
/// HSTS is configured separately via UseHsts() for proper preload support.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // CSP: Allow self-origin, inline styles (for UI frameworks), data URIs for images,
    // and external API endpoints for SendGrid/Twilio
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "connect-src 'self' https://api.sendgrid.com https://api.twilio.com wss:; " +
        "font-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        var headers = context.Response.Headers;

        // Content Security Policy - prevents XSS, clickjacking
        headers.TryAdd("Content-Security-Policy", ContentSecurityPolicy);

        // Prevent MIME type sniffing
        headers.TryAdd("X-Content-Type-Options", "nosniff");

        // Prevent clickjacking - frame embedding denied
        headers.TryAdd("X-Frame-Options", "DENY");

        // Referrer Policy - limit referrer information leakage
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy - disable unnecessary browser features
        headers.TryAdd("Permissions-Policy",
            "geolocation=(), microphone=(), camera=(), payment=(), usb=()");

        // Cache control for sensitive endpoints
        if (context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Path.StartsWithSegments("/api/health"))
        {
            headers.TryAdd("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
            headers.TryAdd("Pragma", "no-cache");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering SecurityHeadersMiddleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the application pipeline.
    /// Should be called early in the pipeline, after UseHsts() if HTTPS is enabled.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
