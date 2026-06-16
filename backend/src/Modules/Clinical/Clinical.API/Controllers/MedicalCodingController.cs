using System.Security.Claims;
using Clinical.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Clinical.API.Controllers;

/// <summary>
/// Medical coding endpoints for ICD-10 and CPT code mapping (SCR-018, AIR-004, AIR-005).
/// Provides AI-driven code suggestions with staff verification workflow.
/// </summary>
[ApiController]
[Route("api/clinical/codes")]
[Authorize]
[Produces("application/json")]
public class MedicalCodingController : ControllerBase
{
    private readonly IIcd10MappingService _icd10MappingService;
    private readonly ICptMappingService _cptMappingService;
    private readonly ICodeVerificationService _verificationService;
    private readonly ILogger<MedicalCodingController> _logger;

    public MedicalCodingController(
        IIcd10MappingService icd10MappingService,
        ICptMappingService cptMappingService,
        ICodeVerificationService verificationService,
        ILogger<MedicalCodingController> logger)
    {
        _icd10MappingService = icd10MappingService;
        _cptMappingService = cptMappingService;
        _verificationService = verificationService;
        _logger = logger;
    }

    /// <summary>
    /// Maps patient diagnoses to ICD-10-CM codes using AI (AC-1, AC-2).
    /// Returns top-3 candidates ranked by confidence with "Suggested" status.
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of ICD-10 mapping results with candidates.</returns>
    /// <response code="200">Mapping completed successfully.</response>
    /// <response code="400">No diagnoses available for mapping.</response>
    /// <response code="403">Not authorized to access patient data.</response>
    [HttpPost("icd10/{patientId:guid}")]
    [ProducesResponseType(typeof(Icd10MappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MapDiagnosesToIcd10(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Verify access: patients can map their own diagnoses, staff can map any
        if (!CanAccessPatient(patientId))
        {
            return Problem(
                detail: "You are not authorized to access this patient's data.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied");
        }

        var result = await _icd10MappingService.MapDiagnosesAsync(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mapping Failed");
        }

        if (result.Value!.Count == 0)
        {
            return Ok(new Icd10MappingResponse(
                Array.Empty<Icd10MappingResultDto>(),
                Message: "No diagnoses available for coding.",
                TotalTokensUsed: 0));
        }

        var response = new Icd10MappingResponse(
            result.Value.Select(r => new Icd10MappingResultDto(
                r.ExtractedDataId,
                r.SourceDiagnosis,
                r.Candidates.Select(c => new Icd10CandidateDto(c.Code, c.Description, c.Confidence)).ToList(),
                r.AllBelowThreshold)).ToList(),
            Message: $"Mapped {result.Value.Count} diagnosis(es) to ICD-10-CM codes.",
            TotalTokensUsed: result.Value.Sum(r => r.TokensUsed));

        return Ok(response);
    }

    /// <summary>
    /// Maps patient procedures to CPT codes using AI (AIR-005).
    /// Returns top-3 candidates ranked by confidence with "Suggested" status.
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of CPT mapping results with candidates.</returns>
    /// <response code="200">Mapping completed successfully.</response>
    /// <response code="400">No procedures available for mapping.</response>
    /// <response code="403">Not authorized to access patient data.</response>
    [HttpPost("cpt/{patientId:guid}")]
    [ProducesResponseType(typeof(CptMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MapProceduresToCpt(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Verify access: patients can map their own procedures, staff can map any
        if (!CanAccessPatient(patientId))
        {
            return Problem(
                detail: "You are not authorized to access this patient's data.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied");
        }

        var result = await _cptMappingService.MapProceduresAsync(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mapping Failed");
        }

        if (result.Value!.Count == 0)
        {
            return Ok(new CptMappingResponse(
                Array.Empty<CptMappingResultDto>(),
                Message: "No procedures available for coding.",
                TotalTokensUsed: 0));
        }

        var response = new CptMappingResponse(
            result.Value.Select(r => new CptMappingResultDto(
                r.ExtractedDataId,
                r.SourceProcedure,
                r.Candidates.Select(c => new CptCandidateDto(c.Code, c.Description, c.Confidence)).ToList(),
                r.AllBelowThreshold)).ToList(),
            Message: $"Mapped {result.Value.Count} procedure(s) to CPT codes.",
            TotalTokensUsed: result.Value.Sum(r => r.TokensUsed));

        return Ok(response);
    }

    /// <summary>
    /// Gets the code verification queue for staff (SCR-018).
    /// Returns pending MedicalCode records requiring verification.
    /// </summary>
    /// <param name="status">Optional filter by verification status.</param>
    /// <param name="codeType">Optional filter by code type (ICD-10 or CPT).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of codes pending verification.</returns>
    [HttpGet("verification-queue")]
    [Authorize(Roles = "FrontDesk,Provider,Admin")]
    [ProducesResponseType(typeof(CodeVerificationQueueResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerificationQueue(
        [FromQuery] string? status = null,
        [FromQuery] string? codeType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _icd10MappingService.GetVerificationQueueAsync(status, codeType, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Query Failed");
        }

        var queue = result.Value!;
        var entries = queue.Entries.Select(e => new CodeVerificationEntryDto(
            e.Id,
            e.PatientId,
            e.PatientName,
            e.CodeType,
            new Icd10CandidateDto(e.PrimaryCode.Code, e.PrimaryCode.Description, e.PrimaryCode.Confidence),
            e.AlternativeCandidates.Select(c => new Icd10CandidateDto(c.Code, c.Description, c.Confidence)).ToList(),
            e.Status,
            e.ExtractedAt)).ToList();

        var response = new CodeVerificationQueueResponse(
            entries,
            TotalCount: queue.TotalCount,
            PendingCount: queue.PendingCount);

        return Ok(response);
    }

    /// <summary>
    /// Verifies (accepts or rejects) a code entry (UXR-602).
    /// </summary>
    /// <param name="entryId">MedicalCode ID.</param>
    /// <param name="request">Verification action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated code entry status.</returns>
    [HttpPost("{entryId:guid}/verify")]
    [Authorize(Roles = "FrontDesk,Provider,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyCode(
        Guid entryId,
        [FromBody] VerifyCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdClaim, out var userId);

        var result = await _icd10MappingService.VerifyCodeAsync(
            entryId,
            userId,
            request.Action,
            request.Reason,
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error!.Contains("not found")
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status409Conflict;
            
            return Problem(
                detail: result.Error,
                statusCode: statusCode,
                title: statusCode == StatusCodes.Status404NotFound ? "Not Found" : "Already Verified");
        }

        _logger.LogInformation(
            "Code {CodeId} {Action} by user {UserId}",
            entryId, request.Action, userId);

        return Ok(new { status = result.Value!.Status });
    }

    /// <summary>
    /// Verifies a code with full action support: accept, reject, or override (AIR-S04).
    /// Supports preserving AI suggestion alongside manual override (AC-5).
    /// </summary>
    /// <param name="codeId">MedicalCode ID.</param>
    /// <param name="request">Verification action details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result with updated code details.</returns>
    /// <response code="200">Code verified successfully.</response>
    /// <response code="400">Invalid verification request.</response>
    /// <response code="404">Code not found.</response>
    /// <response code="409">Code already verified.</response>
    [HttpPut("{codeId:guid}/verify")]
    [Authorize(Roles = "FrontDesk,Provider,Admin")]
    [ProducesResponseType(typeof(VerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyCodeWithOverride(
        Guid codeId,
        [FromBody] VerifyCodeActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Problem(
                detail: "User ID claim not found.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid User");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var actionRequest = new VerificationActionRequest
        {
            Action = request.Action switch
            {
                "accept" => VerificationAction.Accept,
                "reject" => VerificationAction.Reject,
                "override" => VerificationAction.Override,
                _ => VerificationAction.Accept
            },
            RejectionReason = request.RejectionReason,
            OverrideCode = request.OverrideCode,
            OverrideDescription = request.OverrideDescription,
            OverrideReason = request.OverrideReason,
            Notes = request.Notes
        };

        var result = await _verificationService.VerifyCodeAsync(
            codeId,
            userId,
            actionRequest,
            ipAddress,
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error!.Contains("not found")
                ? StatusCodes.Status404NotFound
                : result.Error.Contains("already")
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

            return Problem(
                detail: result.Error,
                statusCode: statusCode,
                title: statusCode switch
                {
                    StatusCodes.Status404NotFound => "Not Found",
                    StatusCodes.Status409Conflict => "Already Verified",
                    _ => "Bad Request"
                });
        }

        _logger.LogInformation(
            "Code {CodeId} {Action}{Override} by user {UserId}",
            codeId,
            request.Action,
            result.Value!.WasOverridden ? $" (overridden from {result.Value.OriginalAiCode})" : "",
            userId);

        return Ok(new VerificationResultDto(
            result.Value.CodeId,
            result.Value.Status,
            result.Value.Code,
            result.Value.Description,
            result.Value.VerifiedAt,
            result.Value.WasOverridden,
            result.Value.OriginalAiCode,
            result.Value.OriginalAiDescription));
    }

    /// <summary>
    /// Gets the rolling 30-day agreement rate metrics (AIR-Q01).
    /// Target: >98% agreement rate for AI code suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agreement rate metrics with trend data.</returns>
    [HttpGet("agreement-rate")]
    [Authorize(Roles = "FrontDesk,Provider,Admin")]
    [ProducesResponseType(typeof(AgreementRateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgreementRate(CancellationToken cancellationToken = default)
    {
        var result = await _verificationService.GetAgreementRateAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Metrics Query Failed");
        }

        var metrics = result.Value!;
        return Ok(new AgreementRateDto(
            metrics.AgreementRate,
            metrics.AcceptedCount,
            metrics.RejectedCount,
            metrics.OverriddenCount,
            metrics.PendingCount,
            metrics.TotalProcessed,
            metrics.PeriodStart,
            metrics.PeriodEnd,
            metrics.TargetMet,
            0.0, // Trend can be added later if needed
            Array.Empty<DailyRateDto>())); // Daily rates from statistics endpoint
    }

    /// <summary>
    /// Gets verification statistics for dashboard (AIR-Q01).
    /// </summary>
    /// <param name="days">Number of days to include (default 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification statistics with daily breakdown.</returns>
    [HttpGet("verification-statistics")]
    [Authorize(Roles = "FrontDesk,Provider,Admin")]
    [ProducesResponseType(typeof(VerificationStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerificationStatistics(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _verificationService.GetVerificationStatisticsAsync(days, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Statistics Query Failed");
        }

        var stats = result.Value!;
        return Ok(new VerificationStatisticsDto(
            stats.DailyCounts.Select(d => new DailyCountDto(
                d.Date, d.AcceptedCount, d.RejectedCount, d.OverriddenCount)).ToList(),
            stats.AverageVerificationTimeHours,
            stats.TopRejectionReasons.Select(r => new RejectionReasonDto(r.Reason, r.Count)).ToList()));
    }

    private bool CanAccessPatient(Guid patientId)
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

        // Staff roles can access any patient
        if (userRoles.Any(r => r is "FrontDesk" or "Provider" or "Admin"))
        {
            return true;
        }

        // Patients can only access their own data
        var patientIdClaim = User.FindFirstValue("PatientId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(patientIdClaim, out var claimPatientId) && claimPatientId == patientId;
    }
}

// Response DTOs

public sealed record Icd10MappingResponse(
    IReadOnlyList<Icd10MappingResultDto> Results,
    string Message,
    int TotalTokensUsed);

public sealed record Icd10MappingResultDto(
    Guid ExtractedDataId,
    string SourceDiagnosis,
    IReadOnlyList<Icd10CandidateDto> Candidates,
    bool AllBelowThreshold);

public sealed record Icd10CandidateDto(
    string Code,
    string Description,
    double Confidence);

public sealed record CodeVerificationQueueResponse(
    IReadOnlyList<CodeVerificationEntryDto> Entries,
    int TotalCount,
    int PendingCount);

public sealed record CodeVerificationEntryDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string CodeType,
    Icd10CandidateDto PrimaryCode,
    IReadOnlyList<Icd10CandidateDto> AlternativeCandidates,
    string Status,
    DateTime ExtractedAt);

public sealed record VerifyCodeRequest(
    string Action,
    string? Reason = null,
    string? Notes = null);

// CPT Response DTOs

public sealed record CptMappingResponse(
    IReadOnlyList<CptMappingResultDto> Results,
    string Message,
    int TotalTokensUsed);

public sealed record CptMappingResultDto(
    Guid ExtractedDataId,
    string SourceProcedure,
    IReadOnlyList<CptCandidateDto> Candidates,
    bool AllBelowThreshold);

public sealed record CptCandidateDto(
    string Code,
    string Description,
    double Confidence);

// Verification Action DTOs

/// <summary>
/// Request model for verification actions with override support.
/// </summary>
public sealed record VerifyCodeActionRequest(
    string Action,
    string? RejectionReason = null,
    string? OverrideCode = null,
    string? OverrideDescription = null,
    string? OverrideReason = null,
    string? Notes = null);

/// <summary>
/// Verification result with override details.
/// </summary>
public sealed record VerificationResultDto(
    Guid CodeId,
    string Status,
    string Code,
    string Description,
    DateTime VerifiedAt,
    bool WasOverridden,
    string? OriginalAiCode,
    string? OriginalAiDescription);

// Agreement Rate DTOs

/// <summary>
/// Rolling 30-day agreement rate metrics.
/// </summary>
public sealed record AgreementRateDto(
    double Rate,
    int AcceptedCount,
    int RejectedCount,
    int OverriddenCount,
    int PendingCount,
    int TotalProcessed,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    bool TargetMet,
    double Trend,
    IReadOnlyList<DailyRateDto> DailyRates);

/// <summary>
/// Daily agreement rate data point.
/// </summary>
public sealed record DailyRateDto(DateTime Date, double Rate);

// Verification Statistics DTOs

/// <summary>
/// Verification statistics for dashboard.
/// </summary>
public sealed record VerificationStatisticsDto(
    IReadOnlyList<DailyCountDto> DailyCounts,
    double AverageVerificationTimeHours,
    IReadOnlyList<RejectionReasonDto> TopRejectionReasons);

/// <summary>
/// Daily verification count breakdown.
/// </summary>
public sealed record DailyCountDto(
    DateTime Date,
    int AcceptedCount,
    int RejectedCount,
    int OverriddenCount);

/// <summary>
/// Rejection reason with count.
/// </summary>
public sealed record RejectionReasonDto(string Reason, int Count);
