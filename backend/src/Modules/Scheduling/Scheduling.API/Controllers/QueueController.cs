using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.DTOs;
using SharedKernel.Audit;

namespace Scheduling.API.Controllers;

/// <summary>
/// Same-day queue management endpoints for staff (AC-1 through AC-5).
/// </summary>
[ApiController]
[Route("api/scheduling/queue")]
[Authorize(Policy = "StaffPolicy")]
[Produces("application/json")]
public class QueueController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly IAuditService? _auditService;
    private readonly ILogger<QueueController> _logger;

    public QueueController(
        IQueueService queueService,
        ILogger<QueueController> logger,
        IAuditService? auditService = null)
    {
        _queueService = queueService;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Returns today's queue summary with entries and statistics (AC-1, AC-4).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(QueueSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueSummary(CancellationToken cancellationToken = default)
    {
        var queue = await _queueService.GetTodayQueueAsync(cancellationToken);
        var entries = queue.ToList();
        
        var waitingCount = entries.Count(e => e.Status == "Waiting");
        var inProgressCount = entries.Count(e => e.Status == "InProgress");
        var completedCount = entries.Count(e => e.Status == "Completed");
        
        // Calculate average wait time for completed entries
        var completedEntries = entries.Where(e => e.Status == "Completed" && e.WaitDurationMinutes > 0).ToList();
        var averageWaitMinutes = completedEntries.Count > 0 
            ? (int)completedEntries.Average(e => e.WaitDurationMinutes) 
            : 0;
        
        return Ok(new QueueSummaryDto
        {
            Entries = entries,
            WaitingCount = waitingCount,
            InProgressCount = inProgressCount,
            CompletedCount = completedCount,
            AverageWaitMinutes = averageWaitMinutes
        });
    }

    /// <summary>
    /// Returns today's queue ordered by arrival time with status indicators (AC-1, AC-4).
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(IReadOnlyList<QueueEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodayQueue(CancellationToken cancellationToken = default)
    {
        var queue = await _queueService.GetTodayQueueAsync(cancellationToken);
        return Ok(queue);
    }

    /// <summary>
    /// Updates the status of a queue entry with optimistic concurrency (AC-2, AC-5).
    /// Valid transitions: Waiting → InProgress | Left | NoShow; InProgress → Completed | Left | NoShow.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(QueueEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] StatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffUserId();
        if (staffId is null)
        {
            return Problem(
                detail: "Unable to determine staff identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var staffName = User.FindFirstValue("name")
                        ?? User.FindFirstValue(ClaimTypes.Name)
                        ?? "Unknown Staff";

        var result = await _queueService.UpdateStatusAsync(
            id,
            request.NewStatus,
            request.RowVersion,
            staffId.Value,
            staffName,
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "CONFLICT")
            {
                return Problem(
                    detail: "This queue entry was modified by another user. Please refresh and try again.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Concurrency Conflict");
            }

            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found");
            }

            _logger.LogWarning("Queue status update failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Status Transition");
        }

        // Audit log for queue status change
        if (_auditService != null)
        {
            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = staffId,
                ActorName = staffName,
                Action = "UpdateQueueStatus",
                Resource = "QueueEntry",
                ResourceId = id.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { id, request.NewStatus }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Marks a same-day appointment as "Arrived" with UTC timestamp (AC-1, AC-4, AC-5).
    /// Rejects cancelled, already-arrived, or future-date appointments.
    /// </summary>
    [HttpPut("~/api/scheduling/appointments/{id:guid}/arrive")]
    [ProducesResponseType(typeof(QueueEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkArrived(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var staffId = GetStaffUserId();
        if (staffId is null)
        {
            return Problem(
                detail: "Unable to determine staff identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var staffName = User.FindFirstValue("name")
                        ?? User.FindFirstValue(ClaimTypes.Name)
                        ?? "Unknown Staff";

        var result = await _queueService.MarkArrivedAsync(
            id,
            staffId.Value,
            staffName,
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found");
            }

            _logger.LogWarning("Arrival marking failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        // Audit log for marking patient arrived
        if (_auditService != null)
        {
            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = staffId,
                ActorName = staffName,
                Action = "MarkPatientArrived",
                Resource = "Appointment",
                ResourceId = id.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { AppointmentId = id, Status = "Arrived" }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the authenticated patient's current queue status (waiting position, wait time).
    /// Returns 404 if patient is not currently in the queue.
    /// </summary>
    [HttpGet("my")]
    [Authorize] // Override class-level StaffPolicy - allow any authenticated user
    [ProducesResponseType(typeof(QueueEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyQueueStatus(CancellationToken cancellationToken = default)
    {
        var patientId = GetPatientId();
        if (patientId is null)
        {
            return Problem(
                detail: "Unable to determine patient identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var entry = await _queueService.GetPatientQueueEntryAsync(patientId.Value, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    private Guid? GetStaffUserId()
    {
        var sub = User.FindFirstValue("sub")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private Guid? GetPatientId()
    {
        var patientIdClaim = User.FindFirstValue("PatientId");
        return Guid.TryParse(patientIdClaim, out var patientId) ? patientId : null;
    }
}
