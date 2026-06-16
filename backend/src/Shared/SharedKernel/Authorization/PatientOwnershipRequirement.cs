using Microsoft.AspNetCore.Authorization;

namespace SharedKernel.Authorization;

/// <summary>
/// Authorization requirement for validating patient data ownership (NFR-009).
/// Ensures patients can only access their own data; staff/admin can access scoped patient data.
/// </summary>
public sealed class PatientOwnershipRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the claim type used to identify the patient from the authenticated user's claims.
    /// </summary>
    public string PatientIdClaimType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatientOwnershipRequirement"/> class.
    /// </summary>
    /// <param name="patientIdClaimType">The claim type for patient identification. Defaults to "PatientId".</param>
    public PatientOwnershipRequirement(string patientIdClaimType = "PatientId")
    {
        PatientIdClaimType = patientIdClaimType;
    }
}

/// <summary>
/// Resource wrapper for patient-specific data used in resource-based authorization.
/// </summary>
public sealed record PatientResource(Guid PatientId, string? ResourceType = null);
