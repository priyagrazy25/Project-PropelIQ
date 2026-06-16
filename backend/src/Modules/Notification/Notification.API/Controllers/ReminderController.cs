using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Services;

namespace Notification.API.Controllers;

/// <summary>
/// Manual reminder endpoints for proactive patient outreach (SCR-022).
/// </summary>
[ApiController]
[Route("api/notification/reminders")]
[Authorize(Roles = "Admin,Provider,FrontDesk")]
[Produces("application/json")]
public class ReminderController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public ReminderController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    /// <summary>
    /// Sends an immediate reminder for a specific appointment.
    /// Used for manual outreach to high-risk patients from the risk dashboard.
    /// </summary>
    /// <param name="appointmentId">The appointment ID to send a reminder for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    /// <response code="204">Reminder sent successfully.</response>
    /// <response code="404">Appointment not found.</response>
    /// <response code="500">Failed to send reminder.</response>
    [HttpPost("{appointmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendReminder(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var (success, error) = await _reminderService.SendImmediateReminderAsync(
            appointmentId, cancellationToken);

        if (success)
        {
            return NoContent();
        }

        if (error == "Appointment not found")
        {
            return Problem(
                detail: error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Problem(
            detail: error ?? "Failed to send reminder",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Reminder Failed");
    }
}
