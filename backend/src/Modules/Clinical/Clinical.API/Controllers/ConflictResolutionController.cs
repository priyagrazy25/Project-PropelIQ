using System.Security.Claims;
using Clinical.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Clinical.API.Controllers;

/// <summary>
/// Conflict resolution endpoints (SCR-017, AC-2, AC-3).
/// Provides conflict details and resolution actions with audit trail.
/// </summary>
[ApiController]
[Route("api/clinical/conflicts")]
[Authorize]
[Produces("application/json")]
public class ConflictResolutionController : ControllerBase
{
    private readonly IConflictResolutionService _resolutionService;
    private readonly ILogger<ConflictResolutionController> _logger;

    public ConflictResolutionController(
        IConflictResolutionService resolutionService,
        ILogger<ConflictResolutionController> logger)
    {
        _resolutionService = resolutionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets detailed conflict information for resolution UI (AC-1).
    /// </summary>
    /// <param name="conflictId">Conflict ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conflict details with source information.</returns>
    /// <response code="200">Conflict details returned successfully.</response>
    /// <response code="404">Conflict not found.</response>
    /// <response code="409">Conflict was already resolved.</response>
    [HttpGet("{conflictId:guid}")]
    [ProducesResponseType(typeof(ConflictDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetConflictDetail(
        Guid conflictId,
        CancellationToken cancellationToken = default)
    {
        var result = await _resolutionService.GetConflictDetailAsync(conflictId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error ?? "Unknown error",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        var conflictDetail = result.Value!;

        // Check if already resolved and return 409 if so
        if (conflictDetail.ResolutionStatus != "Open")
        {
            return Problem(
                detail: "This conflict has already been resolved.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict Already Resolved");
        }

        return Ok(conflictDetail);
    }

    /// <summary>
    /// Resolves a data conflict with the specified action (AC-2).
    /// Creates audit record with before/after state (AC-3).
    /// </summary>
    /// <param name="conflictId">Conflict ID.</param>
    /// <param name="request">Resolution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolution result.</returns>
    /// <response code="200">Conflict resolved successfully.</response>
    /// <response code="400">Invalid request (e.g., manual value required).</response>
    /// <response code="404">Conflict not found.</response>
    /// <response code="409">Conflict was already resolved by another user.</response>
    [HttpPost("{conflictId:guid}/resolve")]
    [ProducesResponseType(typeof(ConflictResolutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveConflict(
        Guid conflictId,
        [FromBody] ResolveConflictApiRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        if (request.ResolutionSource == "manual" && string.IsNullOrWhiteSpace(request.ResolvedValue))
        {
            return Problem(
                detail: "Manual value is required for manual override resolution.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        // Map API request to service request
        var action = request.ResolutionSource?.ToLowerInvariant() switch
        {
            "a" => ResolutionAction.AcceptA,
            "b" => ResolutionAction.AcceptB,
            "manual" => ResolutionAction.ManualOverride,
            _ => throw new ArgumentException($"Invalid resolution source: {request.ResolutionSource}")
        };

        var serviceRequest = new ResolveConflictRequest(
            ConflictId: conflictId,
            Action: action,
            ManualValue: request.ResolvedValue,
            Notes: request.Notes);

        // Get user info from claims
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _resolutionService.ResolveConflictAsync(
            serviceRequest,
            userId,
            userName,
            ipAddress,
            cancellationToken);

        if (!result.IsSuccess)
        {
            // Check for optimistic concurrency error
            if (result.Error?.StartsWith("CONFLICT_ALREADY_RESOLVED") == true)
            {
                return Problem(
                    detail: result.Error.Replace("CONFLICT_ALREADY_RESOLVED: ", ""),
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict Already Resolved");
            }

            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        _logger.LogInformation(
            "User {UserId} resolved conflict {ConflictId} with action {Action}",
            userId, conflictId, action);

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the current user ID from claims.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Gets the current user name from claims.
    /// </summary>
    private string GetCurrentUserName()
    {
        return User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "Unknown";
    }
}

/// <summary>
/// API request model for conflict resolution.
/// </summary>
public sealed record ResolveConflictApiRequest(
    string ResolvedValue,
    string ResolutionSource,
    string? Notes);
