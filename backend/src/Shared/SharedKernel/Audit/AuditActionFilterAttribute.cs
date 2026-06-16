using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Audit;

/// <summary>
/// Action filter attribute that automatically logs API actions to the audit trail.
/// Apply to controllers or actions that require audit logging (FR-031).
/// </summary>
/// <example>
/// [AuditAction("ViewPatient", "Patient")]
/// public async Task&lt;IActionResult&gt; GetPatient(Guid id) { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuditActionFilterAttribute : ActionFilterAttribute
{
    private readonly string _action;
    private readonly string _resource;

    /// <summary>
    /// Creates a new audit action filter.
    /// </summary>
    /// <param name="action">The action being performed (e.g., "Create", "View", "Update", "Delete").</param>
    /// <param name="resource">The resource type (e.g., "Appointment", "Patient", "User").</param>
    public AuditActionFilterAttribute(string action, string resource)
    {
        _action = action;
        _resource = resource;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetService<ILogger<AuditActionFilterAttribute>>();

        // Get user info
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                       ?? httpContext.User.FindFirst("name")?.Value
                       ?? httpContext.User.Identity?.Name
                       ?? "Anonymous";

        // Get resource ID from route/query
        var resourceId = context.ActionArguments.TryGetValue("id", out var idValue)
            ? idValue?.ToString()
            : context.RouteData.Values.TryGetValue("id", out var routeId)
                ? routeId?.ToString()
                : null;

        // Get IP address
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var correlationId = httpContext.TraceIdentifier;

        // Capture before state for updates/deletes (simplified - just captures request body)
        object? beforeState = null;
        if (context.ActionArguments.Count > 0 && _action is "Update" or "Delete")
        {
            // For updates, the current state would need to be fetched from DB
            // This is a simplified version that just logs the request
            beforeState = new { note = "State before action" };
        }

        // Execute the action
        var resultContext = await next();

        // Only log on successful actions
        if (resultContext.Exception == null)
        {
            try
            {
                var auditService = httpContext.RequestServices.GetService<IAuditService>();
                if (auditService != null)
                {
                    // Capture after state from result
                    object? afterState = null;
                    if (resultContext.Result is ObjectResult objectResult && objectResult.Value != null)
                    {
                        afterState = objectResult.Value;
                    }

                    var entry = new AuditEntry
                    {
                        ActorId = Guid.TryParse(userId, out var uid) ? uid : null,
                        ActorName = userName,
                        Action = _action,
                        Resource = _resource,
                        ResourceId = resourceId,
                        BeforeState = beforeState,
                        AfterState = afterState,
                        IpAddress = ipAddress,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    };

                    await auditService.LogActionAsync(entry);
                    
                    logger?.LogDebug(
                        "Audit logged: {Action} on {Resource}/{ResourceId} by {Actor}",
                        _action, _resource, resourceId, userName);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the request if audit logging fails
                logger?.LogWarning(ex, "Failed to log audit entry for {Action} on {Resource}", _action, _resource);
            }
        }
    }
}
