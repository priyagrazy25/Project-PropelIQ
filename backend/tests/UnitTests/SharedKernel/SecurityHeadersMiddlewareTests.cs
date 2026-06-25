using Microsoft.AspNetCore.Http;
using SharedKernel.Middleware;

namespace UnitTests.SharedKernel;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsSecurityHeadersAndNoCacheForApiRoutes()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/scheduling/queue";

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
        Assert.Equal("no-store, no-cache, must-revalidate, proxy-revalidate", context.Response.Headers["Cache-Control"].ToString());
        Assert.Equal("no-cache", context.Response.Headers["Pragma"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_DoesNotAddNoCacheForHealthEndpoint()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/health/ready";

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("Cache-Control"));
        Assert.False(context.Response.Headers.ContainsKey("Pragma"));
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }
}
