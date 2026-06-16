using System.Security.Claims;

namespace SharedKernel.Authorization;

/// <summary>
/// Provides staff data scope context based on the authenticated user's claims.
/// Used to restrict data access for Provider role to their own appointments
/// and enable future facility-based scoping for FrontDesk/Staff roles.
/// </summary>
public interface IStaffDataScopeService
{
    /// <summary>
    /// Gets the current user's role (Admin, Provider, FrontDesk, Staff, Patient).
    /// </summary>
    string? GetCurrentUserRole();

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    Guid? GetCurrentUserId();

    /// <summary>
    /// Gets the ProviderId if the current user is a Provider.
    /// </summary>
    Guid? GetCurrentProviderId();

    /// <summary>
    /// Gets the PatientId if the current user is a Patient.
    /// </summary>
    Guid? GetCurrentPatientId();

    /// <summary>
    /// Returns true if the current user has admin-level access (no data restrictions).
    /// </summary>
    bool HasUnrestrictedAccess();

    /// <summary>
    /// Returns true if the current user can access the specified patient's data.
    /// </summary>
    bool CanAccessPatientData(Guid patientId);

    /// <summary>
    /// Returns true if the current user can access the specified provider's data.
    /// </summary>
    bool CanAccessProviderData(Guid providerId);
}
