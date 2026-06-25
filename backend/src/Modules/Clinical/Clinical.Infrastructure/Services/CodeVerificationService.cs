using Clinical.Application.Abstractions;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using System.Text.Json;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Implements medical code verification workflow per AIR-S04.
/// Handles accept/reject/override with audit trail and agreement rate tracking.
/// </summary>
public sealed class CodeVerificationService : ICodeVerificationService
{
    private readonly ClinicalDbContext _dbContext;
    private readonly ILogger<CodeVerificationService> _logger;
    private const double TargetAgreementRate = 98.0;
    private const int RollingDays = 30;

    public CodeVerificationService(
        ClinicalDbContext dbContext,
        ILogger<CodeVerificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<VerificationActionResult>> VerifyCodeAsync(
        Guid codeId,
        Guid userId,
        VerificationActionRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = ValidateRequest(request);
        if (!validationResult.IsSuccess)
        {
            return Result<VerificationActionResult>.Failure(validationResult.Error!);
        }

        // Find the medical code
        var medicalCode = await _dbContext.MedicalCodes
            .FirstOrDefaultAsync(mc => mc.Id == codeId, cancellationToken);

        if (medicalCode is null)
        {
            return Result<VerificationActionResult>.Failure($"Medical code {codeId} not found.");
        }

        // Check if already processed (idempotency)
        if (medicalCode.VerificationStatus is VerificationStatus.Verified
            or VerificationStatus.Rejected
            or VerificationStatus.Overridden)
        {
            return Result<VerificationActionResult>.Failure(
                $"Medical code {codeId} has already been processed with status '{medicalCode.VerificationStatus}'.");
        }

        // Capture before state for audit
        var beforeState = JsonSerializer.Serialize(new
        {
            medicalCode.Code,
            medicalCode.Description,
            medicalCode.VerificationStatus,
            medicalCode.ConfidenceScore
        });

        // Apply verification action
        var now = DateTime.UtcNow;
        var wasOverridden = false;
        string? originalCode = null;
        string? originalDescription = null;

        switch (request.Action)
        {
            case VerificationAction.Accept:
                medicalCode.VerificationStatus = VerificationStatus.Verified;
                _logger.LogInformation("Code {CodeId} accepted by user {UserId}", codeId, userId);
                break;

            case VerificationAction.Reject:
                medicalCode.VerificationStatus = VerificationStatus.Rejected;
                medicalCode.RejectionReason = request.RejectionReason;
                _logger.LogInformation("Code {CodeId} rejected by user {UserId}: {Reason}",
                    codeId, userId, request.RejectionReason);
                break;

            case VerificationAction.Override:
                // Preserve original AI suggestion (AC-5)
                originalCode = medicalCode.Code;
                originalDescription = medicalCode.Description;
                medicalCode.OriginalAiCode = originalCode;
                medicalCode.OriginalAiDescription = originalDescription;
                medicalCode.OriginalAiConfidence = medicalCode.ConfidenceScore;

                // Apply override
                medicalCode.Code = request.OverrideCode!;
                medicalCode.Description = request.OverrideDescription ?? string.Empty;
                medicalCode.OverrideReason = request.OverrideReason;
                medicalCode.OverrideNotes = request.Notes;
                medicalCode.IsOverridden = true;
                medicalCode.VerificationStatus = VerificationStatus.Overridden;
                wasOverridden = true;

                _logger.LogInformation(
                    "Code {CodeId} overridden by user {UserId}: {OriginalCode} → {NewCode}",
                    codeId, userId, originalCode, request.OverrideCode);
                break;
        }

        medicalCode.VerifiedByUserId = userId;
        medicalCode.VerifiedAt = now;

        // Capture after state for audit
        var afterState = JsonSerializer.Serialize(new
        {
            medicalCode.Code,
            medicalCode.Description,
            medicalCode.VerificationStatus,
            medicalCode.ConfidenceScore,
            medicalCode.IsOverridden,
            medicalCode.OriginalAiCode,
            medicalCode.RejectionReason
        });

        // Create audit record
        var auditLog = new AuditLog
        {
            ActorId = userId,
            ActorName = string.Empty, // Will be populated by audit service if available
            Action = $"CodeVerification.{request.Action}",
            Resource = "MedicalCode",
            ResourceId = codeId.ToString(),
            BeforeState = beforeState,
            AfterState = afterState,
            IpAddress = ipAddress,
            CorrelationId = codeId.ToString()
        };

        _dbContext.AuditLogs.Add(auditLog);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict verifying code {CodeId}", codeId);
            return Result<VerificationActionResult>.Failure(
                "The code was modified by another user. Please refresh and try again.");
        }

        return Result<VerificationActionResult>.Success(new VerificationActionResult
        {
            CodeId = codeId,
            Status = medicalCode.VerificationStatus.ToString(),
            Code = medicalCode.Code,
            Description = medicalCode.Description,
            VerifiedAt = now,
            WasOverridden = wasOverridden,
            OriginalAiCode = originalCode,
            OriginalAiDescription = originalDescription
        });
    }

    /// <inheritdoc />
    public async Task<Result<AgreementRateMetrics>> GetAgreementRateAsync(
        CancellationToken cancellationToken = default)
    {
        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddDays(-RollingDays);

        var stats = await _dbContext.MedicalCodes
            .Where(mc => mc.VerifiedAt >= periodStart && mc.VerifiedAt <= periodEnd)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                AcceptedCount = g.Count(mc => mc.VerificationStatus == VerificationStatus.Verified),
                RejectedCount = g.Count(mc => mc.VerificationStatus == VerificationStatus.Rejected),
                OverriddenCount = g.Count(mc => mc.VerificationStatus == VerificationStatus.Overridden)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var pendingCount = await _dbContext.MedicalCodes
            .CountAsync(mc =>
                mc.VerificationStatus == VerificationStatus.Pending ||
                mc.VerificationStatus == VerificationStatus.NeedsReview,
                cancellationToken);

        var acceptedCount = stats?.AcceptedCount ?? 0;
        var rejectedCount = stats?.RejectedCount ?? 0;
        var overriddenCount = stats?.OverriddenCount ?? 0;
        var totalProcessed = acceptedCount + rejectedCount + overriddenCount;

        // Agreement rate: accepted / (accepted + rejected)
        // Overridden codes are considered as "adjusted accepted" - AI got close but needed refinement
        // Per AIR-Q01: target >98% agreement rate
        var agreementBase = acceptedCount + rejectedCount;
        var agreementRate = agreementBase > 0
            ? (double)acceptedCount / agreementBase * 100.0
            : 100.0; // No data = 100% by default

        return Result<AgreementRateMetrics>.Success(new AgreementRateMetrics
        {
            AgreementRate = Math.Round(agreementRate, 2),
            AcceptedCount = acceptedCount,
            RejectedCount = rejectedCount,
            OverriddenCount = overriddenCount,
            PendingCount = pendingCount,
            TotalProcessed = totalProcessed,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TargetMet = agreementRate >= TargetAgreementRate
        });
    }

    /// <inheritdoc />
    public async Task<Result<VerificationStatistics>> GetVerificationStatisticsAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddDays(-days);

        // Load the minimal shape first, then aggregate in memory to avoid provider translation issues.
        var verifiedRows = await _dbContext.MedicalCodes
            .AsNoTracking()
            .Where(mc => mc.VerifiedAt >= periodStart && mc.VerifiedAt <= periodEnd)
            .Select(mc => new { VerifiedAt = mc.VerifiedAt!.Value, mc.VerificationStatus })
            .ToListAsync(cancellationToken);

        var dailyCounts = verifiedRows
            .GroupBy(row => row.VerifiedAt.Date)
            .Select(g => new DailyVerificationCount(
                g.Key,
                g.Count(row => row.VerificationStatus == VerificationStatus.Verified),
                g.Count(row => row.VerificationStatus == VerificationStatus.Rejected),
                g.Count(row => row.VerificationStatus == VerificationStatus.Overridden)))
            .OrderBy(dc => dc.Date)
            .ToList();

        // Calculate average verification time
        var verifiedCodes = await _dbContext.MedicalCodes
            .AsNoTracking()
            .Where(mc =>
                mc.VerifiedAt >= periodStart &&
                mc.VerifiedAt <= periodEnd &&
                mc.VerificationStatus != VerificationStatus.Pending)
            .Select(mc => new { mc.CreatedAt, mc.VerifiedAt })
            .ToListAsync(cancellationToken);

        var avgHours = verifiedCodes.Count > 0
            ? verifiedCodes.Average(mc => (mc.VerifiedAt!.Value - mc.CreatedAt).TotalHours)
            : 0.0;

        // Get top rejection reasons.
        var rejectionReasonRows = await _dbContext.MedicalCodes
            .AsNoTracking()
            .Where(mc =>
                mc.VerifiedAt >= periodStart &&
                mc.VerifiedAt <= periodEnd &&
                mc.VerificationStatus == VerificationStatus.Rejected &&
                mc.RejectionReason != null)
            .Select(mc => mc.RejectionReason!)
            .ToListAsync(cancellationToken);

        var rejectionReasons = rejectionReasonRows
            .GroupBy(reason => reason)
            .Select(g => new RejectionReasonCount(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(5)
            .ToList();

        return Result<VerificationStatistics>.Success(new VerificationStatistics
        {
            DailyCounts = dailyCounts,
            AverageVerificationTimeHours = Math.Round(avgHours, 2),
            TopRejectionReasons = rejectionReasons
        });
    }

    private static Result<bool> ValidateRequest(VerificationActionRequest request)
    {
        return request.Action switch
        {
            VerificationAction.Reject when string.IsNullOrWhiteSpace(request.RejectionReason) =>
                Result<bool>.Failure("Rejection reason is required when rejecting a code."),

            VerificationAction.Override when string.IsNullOrWhiteSpace(request.OverrideCode) =>
                Result<bool>.Failure("Override code is required when overriding AI suggestion."),

            VerificationAction.Override when string.IsNullOrWhiteSpace(request.OverrideReason) =>
                Result<bool>.Failure("Override reason is required when overriding AI suggestion."),

            _ => Result<bool>.Success(true)
        };
    }
}
