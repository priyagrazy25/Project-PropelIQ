using Scheduling.Application.Commands.WalkInBooking;

namespace Scheduling.Application.Abstractions;

/// <summary>
/// Cross-module abstraction for patient lookup and creation from the Identity module.
/// Implemented in the Host project which has access to both modules.
/// </summary>
public interface IPatientLookupService
{
    Task<IReadOnlyList<PatientSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<PatientSearchResult?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientSearchResult> CreateWalkInPatientAsync(
        string fullName,
        string? email,
        string? phone,
        DateOnly? dateOfBirth,
        CancellationToken cancellationToken = default);
}
