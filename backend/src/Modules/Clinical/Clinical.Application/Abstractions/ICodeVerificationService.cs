using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Service for medical code verification workflow (AC-1, AC-2, AC-5).
/// Handles accept/reject/override operations with audit trail.
/// </summary>
public interface ICodeVerificationService
{
    /// <summary>
    /// Verifies a medical code with accept, reject, or override action.
    /// </summary>
    /// <param name="codeId">MedicalCode identifier.</param>
    /// <param name="userId">User performing verification.</param>
    /// <param name="request">Verification request details.</param>
    /// <param name="ipAddress">IP address for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result with updated status.</returns>
    Task<Result<VerificationActionResult>> VerifyCodeAsync(
        Guid codeId,
        Guid userId,
        VerificationActionRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rolling 30-day agreement rate (AIR-Q01).
    /// Agreement = (Verified codes / Total verified+rejected codes) * 100
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agreement rate metrics.</returns>
    Task<Result<AgreementRateMetrics>> GetAgreementRateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets verification statistics for dashboard.
    /// </summary>
    /// <param name="days">Number of days to include (default 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification statistics.</returns>
    Task<Result<VerificationStatistics>> GetVerificationStatisticsAsync(
        int days = 30,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Verification action types per AIR-S04.
/// </summary>
public enum VerificationAction
{
    Accept = 0,
    Reject = 1,
    Override = 2
}

/// <summary>
/// Request model for verification actions.
/// </summary>
public sealed record VerificationActionRequest
{
    /// <summary>
    /// Action to perform: accept, reject, or override.
    /// </summary>
    public required VerificationAction Action { get; init; }

    /// <summary>
    /// Reason for rejection (required when Action is Reject).
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Custom code when overriding AI suggestion (required when Action is Override).
    /// </summary>
    public string? OverrideCode { get; init; }

    /// <summary>
    /// Custom description when overriding AI suggestion.
    /// </summary>
    public string? OverrideDescription { get; init; }

    /// <summary>
    /// Reason for override (required when Action is Override).
    /// </summary>
    public string? OverrideReason { get; init; }

    /// <summary>
    /// Optional notes from the verifier.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Result of a verification action.
/// </summary>
public sealed record VerificationActionResult
{
    public required Guid CodeId { get; init; }
    public required string Status { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required DateTime VerifiedAt { get; init; }
    public bool WasOverridden { get; init; }
    public string? OriginalAiCode { get; init; }
    public string? OriginalAiDescription { get; init; }
}

/// <summary>
/// Agreement rate metrics for dashboard (AIR-Q01).
/// Target: >98% agreement rate.
/// </summary>
public sealed record AgreementRateMetrics
{
    /// <summary>
    /// Rolling 30-day agreement rate as percentage.
    /// </summary>
    public required double AgreementRate { get; init; }

    /// <summary>
    /// Number of codes accepted (verified).
    /// </summary>
    public required int AcceptedCount { get; init; }

    /// <summary>
    /// Number of codes rejected.
    /// </summary>
    public required int RejectedCount { get; init; }

    /// <summary>
    /// Number of codes overridden.
    /// </summary>
    public required int OverriddenCount { get; init; }

    /// <summary>
    /// Number of codes still pending.
    /// </summary>
    public required int PendingCount { get; init; }

    /// <summary>
    /// Total codes processed (accepted + rejected + overridden).
    /// </summary>
    public required int TotalProcessed { get; init; }

    /// <summary>
    /// Start of the 30-day period.
    /// </summary>
    public required DateTime PeriodStart { get; init; }

    /// <summary>
    /// End of the 30-day period.
    /// </summary>
    public required DateTime PeriodEnd { get; init; }

    /// <summary>
    /// Whether the target (>98%) is met.
    /// </summary>
    public required bool TargetMet { get; init; }
}

/// <summary>
/// Verification statistics for dashboard.
/// </summary>
public sealed record VerificationStatistics
{
    /// <summary>
    /// Daily verification counts.
    /// </summary>
    public required IReadOnlyList<DailyVerificationCount> DailyCounts { get; init; }

    /// <summary>
    /// Average time to verify (in hours).
    /// </summary>
    public required double AverageVerificationTimeHours { get; init; }

    /// <summary>
    /// Top rejection reasons.
    /// </summary>
    public required IReadOnlyList<RejectionReasonCount> TopRejectionReasons { get; init; }
}

/// <summary>
/// Daily verification count for charting.
/// </summary>
public sealed record DailyVerificationCount(
    DateTime Date,
    int AcceptedCount,
    int RejectedCount,
    int OverriddenCount);

/// <summary>
/// Rejection reason with count.
/// </summary>
public sealed record RejectionReasonCount(
    string Reason,
    int Count);
