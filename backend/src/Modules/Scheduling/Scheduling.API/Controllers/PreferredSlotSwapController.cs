using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.RegisterSwapPreference;
using Scheduling.Domain.Enums;

namespace Scheduling.API.Controllers;

[ApiController]
[Route("api/scheduling/swap-preferences")]
[Authorize]
[Produces("application/json")]
public class PreferredSlotSwapController : ControllerBase
{
    private readonly RegisterSwapPreferenceCommandHandler _registerHandler;
    private readonly ISchedulingDbContext _dbContext;
    private readonly ILogger<PreferredSlotSwapController> _logger;

    public PreferredSlotSwapController(
        RegisterSwapPreferenceCommandHandler registerHandler,
        ISchedulingDbContext dbContext,
        ILogger<PreferredSlotSwapController> logger)
    {
        _registerHandler = registerHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Registers a preferred slot swap preference for an existing appointment.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegisterSwapPreferenceResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterSwapPreference(
        [FromBody] RegisterSwapPreferenceRequest request,
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

        var command = new RegisterSwapPreferenceCommand(
            patientId.Value,
            request.AppointmentId,
            request.DesiredSlotId);

        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Swap preference registration failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Swap Preference Error");
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Gets active swap preferences for the authenticated patient.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SwapPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSwapPreferences(CancellationToken cancellationToken = default)
    {
        var patientId = GetPatientId();
        if (patientId is null)
        {
            return Problem(
                detail: "Unable to determine patient identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var swaps = await _dbContext.PreferredSlotSwaps
            .AsNoTracking()
            .Include(s => s.DesiredSlot)
                .ThenInclude(slot => slot.Provider)
            .Where(s => s.RequestingPatientId == patientId.Value && !s.IsDeleted)
            .OrderByDescending(s => s.RequestedAt)
            .Select(s => new SwapPreferenceDto(
                s.Id,
                s.OriginalAppointmentId,
                s.DesiredSlotId,
                s.DesiredSlot.Provider.Name,
                s.DesiredSlot.StartTime,
                s.Status.ToString(),
                s.RequestedAt,
                s.ProcessedAt))
            .ToListAsync(cancellationToken);

        return Ok(swaps);
    }

    /// <summary>
    /// Cancels a pending swap preference.
    /// </summary>
    [HttpDelete("{swapId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSwapPreference(
        Guid swapId,
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

        var swap = await _dbContext.PreferredSlotSwaps
            .FirstOrDefaultAsync(
                s => s.Id == swapId
                     && s.RequestingPatientId == patientId.Value
                     && !s.IsDeleted,
                cancellationToken);

        if (swap is null)
        {
            return Problem(
                detail: "Swap preference not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        if (swap.Status != SwapStatus.Pending)
        {
            return Problem(
                detail: "Only pending swap preferences can be cancelled.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Operation");
        }

        swap.Status = SwapStatus.Cancelled;
        swap.ProcessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Swap preference {SwapId} cancelled by patient {PatientId}", swapId, patientId.Value);

        return NoContent();
    }

    private Guid? GetPatientId()
    {
        var sub = User.FindFirstValue("sub")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var patientId) ? patientId : null;
    }
}

public sealed record RegisterSwapPreferenceRequest(
    Guid AppointmentId,
    Guid DesiredSlotId);

public sealed record SwapPreferenceDto(
    Guid Id,
    Guid AppointmentId,
    Guid DesiredSlotId,
    string ProviderName,
    DateTime PreferredSlotStartTime,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
