using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Authorization;

/// <summary>
/// Implements staff data scope filtering based on the authenticated user's claims (NFR-009, FR-032).
/// - Admin/ComplianceOfficer: Unrestricted access for administrative functions
/// - Provider: Restricted to their own appointments (ProviderId claim)
/// - FrontDesk/Staff: Access to scheduling operations (future: facility-scoped)
/// - Patient: Restricted to own data only (PatientId claim)
/// </summary>
public sealed class StaffDataScopeFilter : IStaffDataScopeService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<StaffDataScopeFilter> _logger;

    // Roles with unrestricted data access
    private static readonly HashSet<string> UnrestrictedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "ComplianceOfficer"
    };

    // Roles that can access all patient scheduling data (future: facility-scoped)
    private static readonly HashSet<string> SchedulingStaffRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "FrontDesk",
        "Staff"
    };

    public StaffDataScopeFilter(
        IHttpContextAccessor httpContextAccessor,
        ILogger<StaffDataScopeFilter> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? GetCurrentUserRole()
    {
        return User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public Guid? GetCurrentUserId()
    {
        var subClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst("sub")?.Value;
        
        return Guid.TryParse(subClaim, out var userId) ? userId : null;
    }

    public Guid? GetCurrentProviderId()
    {
        var providerIdClaim = User?.FindFirst("ProviderId")?.Value
                           ?? User?.FindFirst("provider_id")?.Value;
        
        return Guid.TryParse(providerIdClaim, out var providerId) ? providerId : null;
    }

    public Guid? GetCurrentPatientId()
    {
        var patientIdClaim = User?.FindFirst("PatientId")?.Value
                          ?? User?.FindFirst("patient_id")?.Value;
        
        return Guid.TryParse(patientIdClaim, out var patientId) ? patientId : null;
    }

    public bool HasUnrestrictedAccess()
    {
        var role = GetCurrentUserRole();
        return !string.IsNullOrEmpty(role) && UnrestrictedRoles.Contains(role);
    }

    public bool CanAccessPatientData(Guid patientId)
    {
        var role = GetCurrentUserRole();
        
        // Admin/ComplianceOfficer: unrestricted
        if (!string.IsNullOrEmpty(role) && UnrestrictedRoles.Contains(role))
        {
            return true;
        }

        // Provider: can access their own patients' data for care delivery
        if (string.Equals(role, "Provider", StringComparison.OrdinalIgnoreCase))
        {
            // Providers have access to patient data during appointments
            // Future: Could add provider-patient relationship check
            return true;
        }

        // FrontDesk/Staff: can access scheduling data for operations
        if (!string.IsNullOrEmpty(role) && SchedulingStaffRoles.Contains(role))
        {
            // Future: Add facility-based scoping when FacilityId is added
            return true;
        }

        // Patient: can only access own data
        var currentPatientId = GetCurrentPatientId();
        if (currentPatientId.HasValue && currentPatientId.Value == patientId)
        {
            return true;
        }

        _logger.LogWarning(
            "Data scope violation: User {UserId} with role {Role} attempted to access patient {PatientId}",
            GetCurrentUserId(), role, patientId);

        return false;
    }

    public bool CanAccessProviderData(Guid providerId)
    {
        var role = GetCurrentUserRole();

        // Admin/ComplianceOfficer: unrestricted
        if (!string.IsNullOrEmpty(role) && UnrestrictedRoles.Contains(role))
        {
            return true;
        }

        // Provider: can only access own data
        if (string.Equals(role, "Provider", StringComparison.OrdinalIgnoreCase))
        {
            var currentProviderId = GetCurrentProviderId();
            if (currentProviderId.HasValue && currentProviderId.Value == providerId)
            {
                return true;
            }

            _logger.LogWarning(
                "Provider scope violation: Provider {CurrentProviderId} attempted to access provider {TargetProviderId} data",
                currentProviderId, providerId);

            return false;
        }

        // FrontDesk/Staff: can access provider schedules for operations
        if (!string.IsNullOrEmpty(role) && SchedulingStaffRoles.Contains(role))
        {
            return true;
        }

        // Patient: can view provider availability (limited data)
        if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            // Patients can view provider availability for booking
            return true;
        }

        return false;
    }
}
