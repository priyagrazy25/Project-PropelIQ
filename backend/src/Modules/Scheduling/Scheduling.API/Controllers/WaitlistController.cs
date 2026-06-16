using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Services;

namespace Scheduling.API.Controllers;

[ApiController]
[Route("api/scheduling/waitlist")]
[Authorize]
[Produces("application/json")]
public class WaitlistController : ControllerBase
{
    private readonly IWaitlistService _waitlistService;
    private readonly ILogger<WaitlistController> _logger;

    public WaitlistController(
        IWaitlistService waitlistService,
        ILogger<WaitlistController> logger)
    {
        _waitlistService = waitlistService;
        _logger = logger;
    }

    /// <summary>
    /// Enrolls the authenticated patient on the waitlist for a provider with a preferred date range.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WaitlistEntryResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinWaitlist(
        [FromBody] JoinWaitlistRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientId = GetPatientId();
        if (patientId is null)
        {
            return Problem(
                detail: "Unable to determine patient identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var result = await _waitlistService.EnrollAsync(
            patientId.Value,
            request.ProviderId,
            request.PreferredDateStart,
            request.PreferredDateEnd,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Waitlist enrollment failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Waitlist Enrollment Error");
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Gets the authenticated patient's waitlist entries.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<WaitlistEntryResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWaitlistEntries(CancellationToken cancellationToken = default)
    {
        var patientId = GetPatientId();
        if (patientId is null)
        {
            return Problem(
                detail: "Unable to determine patient identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var entries = await _waitlistService.GetPatientEntriesAsync(patientId.Value, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Removes the authenticated patient from a waitlist entry.
    /// </summary>
    [HttpDelete("{waitlistId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromWaitlist(
        Guid waitlistId,
        CancellationToken cancellationToken = default)
    {
        var patientId = GetPatientId();
        if (patientId is null)
        {
            return Problem(
                detail: "Unable to determine patient identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var result = await _waitlistService.RemoveAsync(patientId.Value, waitlistId, cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Problem(
                detail: result.Error,
                statusCode: statusCode,
                title: statusCode == StatusCodes.Status404NotFound ? "Not Found" : "Invalid Operation");
        }

        return NoContent();
    }

    private Guid? GetPatientId()
    {
        var sub = User.FindFirstValue("sub")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var patientId) ? patientId : null;
    }
}

public sealed record JoinWaitlistRequest(
    Guid ProviderId,
    DateTime PreferredDateStart,
    DateTime PreferredDateEnd);
