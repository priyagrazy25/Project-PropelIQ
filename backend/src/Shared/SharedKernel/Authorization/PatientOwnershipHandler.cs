using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace SharedKernel.Authorization;

/// <summary>
/// Authorization handler for resource-based patient data ownership (NFR-009, FR-032).
/// Validates that the authenticated user's PatientId claim matches the requested resource's PatientId.
/// Staff and Admin roles are granted access for legitimate patient care and administrative purposes.
/// </summary>
public sealed class PatientOwnershipHandler
    : AuthorizationHandler<PatientOwnershipRequirement, PatientResource>
{
    private readonly ILogger<PatientOwnershipHandler> _logger;

    // Roles that can access patient data beyond ownership (staff/admin scoped access)
    private static readonly HashSet<string> ElevatedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Provider",
        "FrontDesk",
        "Staff",
        "ComplianceOfficer"
    };

    public PatientOwnershipHandler(ILogger<PatientOwnershipHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PatientOwnershipRequirement requirement,
        PatientResource resource)
    {
        var user = context.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        // Check if user has an elevated role that allows broader access
        if (!string.IsNullOrEmpty(userRole) && ElevatedRoles.Contains(userRole))
        {
            _logger.LogDebug(
                "User {UserId} with role {Role} granted access to patient resource {PatientId}",
                userId, userRole, resource.PatientId);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // For Patient role, validate ownership
        var patientIdClaim = user.FindFirst(requirement.PatientIdClaimType)?.Value
                          ?? user.FindFirst("patient_id")?.Value
                          ?? user.FindFirst("PatientId")?.Value;

        if (string.IsNullOrEmpty(patientIdClaim))
        {
            _logger.LogWarning(
                "Access denied: User {UserId} lacks PatientId claim when accessing patient resource {ResourcePatientId}",
                userId, resource.PatientId);
            context.Fail(new AuthorizationFailureReason(this, "Missing patient identification claim"));
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(patientIdClaim, out var authenticatedPatientId))
        {
            _logger.LogWarning(
                "Access denied: Invalid PatientId claim format for user {UserId}",
                userId);
            context.Fail(new AuthorizationFailureReason(this, "Invalid patient identification claim format"));
            return Task.CompletedTask;
        }

        if (authenticatedPatientId != resource.PatientId)
        {
            _logger.LogWarning(
                "Access denied: Patient {AuthenticatedPatientId} attempted to access data for patient {ResourcePatientId}",
                authenticatedPatientId, resource.PatientId);
            context.Fail(new AuthorizationFailureReason(this, "Patient can only access their own data"));
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "Patient {PatientId} granted access to their own resource",
            resource.PatientId);
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
