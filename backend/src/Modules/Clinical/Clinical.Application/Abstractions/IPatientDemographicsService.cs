namespace Clinical.Application.Abstractions;

/// <summary>
/// Cross-module abstraction for patient demographics lookup (SCR-016).
/// Implemented in the Host project which has access to the Identity module.
/// </summary>
public interface IPatientDemographicsService
{
    /// <summary>
    /// Gets demographics for a patient by their ID.
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Patient demographics or null if not found.</returns>
    Task<PatientDemographicsResult?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Patient demographics result for 360 view.
/// </summary>
public sealed record PatientDemographicsResult(
    Guid PatientId,
    Guid UserId,
    string FullName,
    string? Email,
    string? ContactNumber,
    DateOnly? DateOfBirth,
    string? Address,
    string? InsuranceProvider,
    string? InsurancePolicyNumber);
