using Clinical.Application.DTOs;
using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Service for generating 360-degree patient views (SCR-016, AIR-002).
/// Aggregates extracted data with semantic de-duplication and caching.
/// </summary>
public interface IPatientView360Service
{
    /// <summary>
    /// Gets the aggregated 360-degree view for a patient.
    /// Returns cached response if available (NFR-004, NFR-017).
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated patient view or error.</returns>
    Task<Result<PatientView360Response>> GetPatientView360Async(
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached 360 view for a patient.
    /// Called when new documents are processed.
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateCacheAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
