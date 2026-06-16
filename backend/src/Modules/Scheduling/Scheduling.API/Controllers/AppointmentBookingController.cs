using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.BookAppointment;
using Scheduling.Application.Commands.CancelAppointment;
using Scheduling.Application.Commands.RescheduleAppointment;
using Scheduling.Application.Queries.GetMyAppointments;
using SharedKernel.Audit;
using SharedKernel.Authorization;

namespace Scheduling.API.Controllers;

/// <summary>
/// Appointment booking endpoints for patients.
/// </summary>
[ApiController]
[Route("api/scheduling/appointments")]
[Authorize]
[Produces("application/json")]
public class AppointmentBookingController : ControllerBase
{
    private readonly BookAppointmentCommandHandler _bookingHandler;
    private readonly CancelAppointmentCommandHandler _cancelHandler;
    private readonly RescheduleAppointmentCommandHandler _rescheduleHandler;
    private readonly GetMyAppointmentsQueryHandler _myAppointmentsHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISchedulingDbContext _dbContext;
    private readonly IAuditService? _auditService;
    private readonly ILogger<AppointmentBookingController> _logger;

    public AppointmentBookingController(
        BookAppointmentCommandHandler bookingHandler,
        CancelAppointmentCommandHandler cancelHandler,
        RescheduleAppointmentCommandHandler rescheduleHandler,
        GetMyAppointmentsQueryHandler myAppointmentsHandler,
        IAuthorizationService authorizationService,
        ISchedulingDbContext dbContext,
        ILogger<AppointmentBookingController> logger,
        IAuditService? auditService = null)
    {
        _bookingHandler = bookingHandler;
        _cancelHandler = cancelHandler;
        _rescheduleHandler = rescheduleHandler;
        _myAppointmentsHandler = myAppointmentsHandler;
        _authorizationService = authorizationService;
        _dbContext = dbContext;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Books an appointment for the authenticated patient. Idempotent via idempotency key.
    /// </summary>
    /// <param name="request">Booking request with provider ID, slot ID, and idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Booking confirmation with appointment details.</returns>
    /// <response code="200">Appointment booked successfully (or idempotent duplicate).</response>
    /// <response code="400">Invalid request (missing fields).</response>
    /// <response code="409">Slot already taken by another patient.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BookAppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentRequest request,
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

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Problem(
                detail: "Idempotency key is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var command = new BookAppointmentCommand(
            patientId.Value,
            request.ProviderId,
            request.SlotId,
            request.IdempotencyKey);

        var result = await _bookingHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "CONFLICT")
            {
                return Problem(
                    detail: "This slot is no longer available. Please select another.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Booking Conflict");
            }

            _logger.LogWarning("Booking failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Booking Error");
        }

        // Audit log for appointment booking
        if (_auditService != null)
        {
            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = patientId,
                ActorName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Unknown",
                Action = "BookAppointment",
                Resource = "Appointment",
                ResourceId = result.Value!.AppointmentId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { result.Value.AppointmentId, result.Value.ProviderId, result.Value.SlotStartTime }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancels an appointment. Idempotent: returns 204 if already cancelled (AC-5).
    /// Releases the slot and triggers swap/waitlist cascade.
    /// </summary>
    [HttpDelete("{appointmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelAppointment(
        Guid appointmentId,
        [FromBody] CancelAppointmentRequest? request,
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

        // Resource-based authorization check for patient ownership (NFR-009)
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted, cancellationToken);

        if (appointment is null)
        {
            return Problem(
                detail: "Appointment not found.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Not Found");
        }

        // Check authorization using PatientOwnershipRequirement
        var resource = new PatientResource(appointment.PatientId);
        var authResult = await _authorizationService.AuthorizeAsync(User, resource, new PatientOwnershipRequirement());

        if (!authResult.Succeeded)
        {
            _logger.LogWarning(
                "Access denied: User {UserId} attempted to cancel appointment {AppointmentId} belonging to patient {PatientId}",
                patientId, appointmentId, appointment.PatientId);

            return Problem(
                detail: "You do not have permission to cancel this appointment.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied");
        }

        var isStaff = User.IsInRole("Staff") || User.IsInRole("Admin") || User.IsInRole("FrontDesk") || User.IsInRole("Provider");

        var command = new CancelAppointmentCommand(
            appointment.PatientId, // Use the appointment's actual patient ID
            appointmentId,
            request?.CancellationReason,
            isStaff);

        var result = await _cancelHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Cancellation failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cancellation Error");
        }

        // Audit log for appointment cancellation
        if (_auditService != null)
        {
            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = patientId,
                ActorName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Unknown",
                Action = "CancelAppointment",
                Resource = "Appointment",
                ResourceId = appointmentId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                BeforeState = new { appointmentId, appointment.PatientId, appointment.ProviderId },
                AfterState = new { Status = "Cancelled", request?.CancellationReason }
            }, cancellationToken);
        }

        return NoContent();
    }

    /// <summary>
    /// Reschedules an appointment to a new slot. Atomically books new slot, cancels old,
    /// and triggers swap queue + waitlist cascade for the released slot (AC-2, AC-3).
    /// </summary>
    [HttpPost("{appointmentId:guid}/reschedule")]
    [ProducesResponseType(typeof(BookAppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleAppointment(
        Guid appointmentId,
        [FromBody] RescheduleAppointmentRequest request,
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

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Problem(
                detail: "Idempotency key is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        // Resource-based authorization check for patient ownership (NFR-009)
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted, cancellationToken);

        if (appointment is null)
        {
            return Problem(
                detail: "Appointment not found.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Not Found");
        }

        var resource = new PatientResource(appointment.PatientId);
        var authResult = await _authorizationService.AuthorizeAsync(User, resource, new PatientOwnershipRequirement());

        if (!authResult.Succeeded)
        {
            _logger.LogWarning(
                "Access denied: User {UserId} attempted to reschedule appointment {AppointmentId} belonging to patient {PatientId}",
                patientId, appointmentId, appointment.PatientId);

            return Problem(
                detail: "You do not have permission to reschedule this appointment.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied");
        }

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, // Use the appointment's actual patient ID
            appointmentId,
            request.NewSlotId,
            request.IdempotencyKey,
            request.CancellationReason);

        var result = await _rescheduleHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "CONFLICT")
            {
                return Problem(
                    detail: "The selected slot is no longer available. Please select another.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Reschedule Conflict");
            }

            _logger.LogWarning("Reschedule failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Reschedule Error");
        }

        // Audit log for appointment rescheduling
        if (_auditService != null)
        {
            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = patientId,
                ActorName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Unknown",
                Action = "RescheduleAppointment",
                Resource = "Appointment",
                ResourceId = result.Value!.AppointmentId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                BeforeState = new { OldAppointmentId = appointmentId },
                AfterState = new { result.Value.AppointmentId, result.Value.SlotStartTime, request.NewSlotId }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the authenticated patient's appointments.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<MyAppointmentResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken = default)
    {
        var patientId = GetPatientId();
        if (patientId is null)
        {
            return Problem(
                detail: "Unable to determine patient identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        var result = await _myAppointmentsHandler.HandleAsync(
            new GetMyAppointmentsQuery(patientId.Value), cancellationToken);

        return Ok(result.Value);
    }

    private Guid? GetPatientId()
    {
        var sub = User.FindFirstValue("sub")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(sub, out var patientId))
        {
            return patientId;
        }

        return null;
    }
}

/// <summary>
/// Request body for booking an appointment.
/// </summary>
public sealed record BookAppointmentRequest(
    Guid ProviderId,
    Guid SlotId,
    string IdempotencyKey);

public sealed record CancelAppointmentRequest(
    string? CancellationReason);

public sealed record RescheduleAppointmentRequest(
    Guid NewSlotId,
    string IdempotencyKey,
    string? CancellationReason);
