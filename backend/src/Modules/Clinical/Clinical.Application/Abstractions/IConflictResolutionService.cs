using Clinical.Domain.Entities;
using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Service for resolving data conflicts (AC-2, AC-3, AC-4).
/// Supports accept-a, accept-b, or manual-override actions with audit trail.
/// </summary>
public interface IConflictResolutionService
{
    /// <summary>
    /// Gets detailed conflict information for resolution UI.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conflict details with source information.</returns>
    Task<Result<ConflictDetailResponse>> GetConflictDetailAsync(
        Guid conflictId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the count of open data conflicts (used by staff dashboard).
    /// </summary>
    Task<int> GetOpenConflictCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a data conflict with the specified action (AC-2).
    /// Creates audit record and invalidates cache.
    /// </summary>
    /// <param name="request">Resolution request with action and value.</param>
    /// <param name="userId">ID of the user performing the resolution.</param>
    /// <param name="userName">Name of the user performing the resolution.</param>
    /// <param name="ipAddress">IP address of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolution result.</returns>
    Task<Result<ConflictResolutionResponse>> ResolveConflictAsync(
        ResolveConflictRequest request,
        Guid userId,
        string userName,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolution action types.
/// </summary>
public enum ResolutionAction
{
    /// <summary>Accept Source A value.</summary>
    AcceptA,
    /// <summary>Accept Source B value.</summary>
    AcceptB,
    /// <summary>Manual override with custom value.</summary>
    ManualOverride
}

/// <summary>
/// Request to resolve a conflict.
/// </summary>
public sealed record ResolveConflictRequest(
    Guid ConflictId,
    ResolutionAction Action,
    string? ManualValue,
    string? Notes);

/// <summary>
/// Response after resolving a conflict.
/// </summary>
public sealed record ConflictResolutionResponse(
    Guid ConflictId,
    string ResolvedValue,
    DateTime ResolvedAt,
    string Message);

/// <summary>
/// Detailed conflict information for resolution UI.
/// </summary>
public sealed record ConflictDetailResponse(
    Guid ConflictId,
    Guid PatientId,
    string Field,
    string Category,
    ConflictSourceInfo SourceA,
    ConflictSourceInfo SourceB,
    string Severity,
    string ResolutionStatus,
    string? ResolvedValue,
    DateTime? ResolvedAt,
    string? ResolvedBy,
    string? ResolutionNotes);

/// <summary>
/// Information about a conflict source.
/// </summary>
public sealed record ConflictSourceInfo(
    Guid? DocumentId,
    string DocumentName,
    string Value,
    double Confidence,
    DateTime ExtractedAt);
