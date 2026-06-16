using System.Text.RegularExpressions;
using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

public sealed class InsuranceValidationService : IInsuranceValidationService
{
    private static readonly Regex SanitizePattern = new(@"[^\w\s\-]", RegexOptions.Compiled);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    private readonly IdentityDbContext _identityDb;
    private readonly ClinicalDbContext _clinicalDb;
    private readonly ILogger<InsuranceValidationService> _logger;

    public InsuranceValidationService(
        IdentityDbContext identityDb,
        ClinicalDbContext clinicalDb,
        ILogger<InsuranceValidationService> logger)
    {
        _identityDb = identityDb;
        _clinicalDb = clinicalDb;
        _logger = logger;
    }

    public async Task<Result<InsuranceCheckResult>> ValidateAsync(
        InsuranceCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedName = Sanitize(request.InsuranceName);
        var sanitizedMemberId = Sanitize(request.MemberId);

        if (string.IsNullOrWhiteSpace(sanitizedName) || string.IsNullOrWhiteSpace(sanitizedMemberId))
        {
            return Result<InsuranceCheckResult>.Failure("Insurance name and member ID are required.");
        }

        var hasRecords = await _identityDb.InsurancePlans.AnyAsync(cancellationToken);
        if (!hasRecords)
        {
            return Result<InsuranceCheckResult>.Success(
                new InsuranceCheckResult(InsuranceVerificationStatus.Unrecognized, "Verification unavailable"));
        }

        var matchedPlan = await _identityDb.InsurancePlans
            .AsNoTracking()
            .Where(p => p.IsActive && p.InsuranceName.ToLower() == sanitizedName.ToLower())
            .FirstOrDefaultAsync(cancellationToken);

        InsuranceVerificationStatus status;
        string message;

        if (matchedPlan is null)
        {
            status = InsuranceVerificationStatus.Unrecognized;
            message = "Unverified - Insurance not recognized";
        }
        else if (IsValidMemberId(sanitizedMemberId, matchedPlan.ValidMemberIdPattern))
        {
            status = InsuranceVerificationStatus.Verified;
            message = "Verified";
        }
        else
        {
            status = InsuranceVerificationStatus.PartialMatch;
            message = "Unverified - Member ID mismatch";
        }

        var verification = new InsuranceVerification
        {
            AppointmentId = request.AppointmentId,
            InsuranceName = sanitizedName,
            MemberId = sanitizedMemberId,
            Status = status,
            StatusMessage = message
        };

        _clinicalDb.InsuranceVerifications.Add(verification);
        await _clinicalDb.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Insurance validation for appointment {AppointmentId}: {Status}.",
            request.AppointmentId, status);

        return Result<InsuranceCheckResult>.Success(new InsuranceCheckResult(status, message));
    }

    public async Task<Result<InsuranceCheckResult>> GetStatusAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var verification = await _clinicalDb.InsuranceVerifications
            .AsNoTracking()
            .Where(v => v.AppointmentId == appointmentId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (verification is null)
        {
            return Result<InsuranceCheckResult>.Failure("No insurance verification found for this appointment.");
        }

        return Result<InsuranceCheckResult>.Success(
            new InsuranceCheckResult(verification.Status, verification.StatusMessage));
    }

    private static string Sanitize(string input)
    {
        var trimmed = input.Trim();
        return SanitizePattern.Replace(trimmed, string.Empty);
    }

    public async Task<InsuranceVerifyResponse> VerifyAsync(
        InsuranceVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedName = Sanitize(request.InsuranceName);
        var sanitizedMemberId = Sanitize(request.MemberId);

        if (string.IsNullOrWhiteSpace(sanitizedName) || string.IsNullOrWhiteSpace(sanitizedMemberId))
        {
            return new InsuranceVerifyResponse(
                InsuranceVerifyStatus.Unavailable,
                request.InsuranceName,
                "Verification unavailable",
                "Insurance name and member ID are required.");
        }

        var hasRecords = await _identityDb.InsurancePlans.AnyAsync(cancellationToken);
        if (!hasRecords)
        {
            return new InsuranceVerifyResponse(
                InsuranceVerifyStatus.Unavailable,
                sanitizedName,
                "Verification unavailable",
                "No insurance records found. You may proceed with your visit.");
        }

        var matchedPlan = await _identityDb.InsurancePlans
            .AsNoTracking()
            .Where(p => p.IsActive && p.InsuranceName.ToLower() == sanitizedName.ToLower())
            .FirstOrDefaultAsync(cancellationToken);

        if (matchedPlan is null)
        {
            return new InsuranceVerifyResponse(
                InsuranceVerifyStatus.Unrecognized,
                sanitizedName,
                "Insurance not recognized",
                "Staff follow-up required to confirm insurance details.");
        }

        if (IsValidMemberId(sanitizedMemberId, matchedPlan.ValidMemberIdPattern))
        {
            return new InsuranceVerifyResponse(
                InsuranceVerifyStatus.Verified,
                sanitizedName,
                "Insurance verified successfully");
        }

        return new InsuranceVerifyResponse(
            InsuranceVerifyStatus.Partial,
            sanitizedName,
            "Member ID mismatch",
            "Insurance provider found but member ID could not be verified.");
    }

    private static bool IsValidMemberId(string memberId, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(memberId, pattern, RegexOptions.None, RegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
