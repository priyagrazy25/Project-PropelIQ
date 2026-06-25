using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.WalkInBooking;
using SharedKernel.Audit;

namespace Scheduling.API.Controllers;

/// <summary>
/// Staff-only walk-in booking endpoints (AC-1, AC-4).
/// </summary>
[ApiController]
[Route("api/scheduling/walk-in")]
[Authorize(Policy = "StaffPolicy")]
[Produces("application/json")]
public class WalkInController : ControllerBase
{
    private readonly IWalkInService _walkInService;
    private readonly IPatientLookupService _patientLookup;
    private readonly IAuditService? _auditService;
    private readonly ILogger<WalkInController> _logger;

    public WalkInController(
        IWalkInService walkInService,
        IPatientLookupService patientLookup,
        ILogger<WalkInController> logger,
        IAuditService? auditService = null)
    {
        _walkInService = walkInService;
        _patientLookup = patientLookup;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Searches for existing patients by name or email (AC-2).
    /// </summary>
    /// <param name="q">Search query (name or email).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching patients.</returns>
    /// <response code="200">Matching patients returned.</response>
    /// <response code="400">Search query is empty.</response>
    [HttpGet("patients/search")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientSearchResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string? q,
        CancellationToken cancellationToken = default)
    {

        var results = await _patientLookup.SearchAsync(q?.Trim() ?? string.Empty, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Creates a walk-in appointment for an existing or new patient (AC-1, AC-3, AC-4).
    /// Assigns a same-day slot or enrolls in the walk-in queue with arrival order.
    /// Returns queue position for toast notification (AC-5).
    /// </summary>
    /// <param name="request">Walk-in booking request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Booking result with queue position.</returns>
    /// <response code="200">Walk-in appointment created.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="409">Slot concurrency conflict.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WalkInBookingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookWalkIn(
        [FromBody] WalkInBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var staffUserId = GetStaffUserId();
        if (staffUserId is null)
        {
            return Problem(
                detail: "Unable to determine staff identity.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Identity");
        }

        if (request.ExistingPatientId is null && string.IsNullOrWhiteSpace(request.PatientName))
        {
            return Problem(
                detail: "Either an existing patient ID or a patient name is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var command = new WalkInBookingCommand(
            request.ExistingPatientId,
            request.PatientName,
            request.PatientEmail,
            request.PatientPhone,
            request.PatientDateOfBirth,
            request.ProviderId,
            request.Reason,
            staffUserId.Value);

        var result = await _walkInService.BookWalkInAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "CONFLICT")
            {
                return Problem(
                    detail: "A concurrency conflict occurred. Please retry.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Booking Conflict");
            }

            _logger.LogWarning("Walk-in booking failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Walk-In Booking Error");
        }

        // Audit log for walk-in booking
        if (_auditService != null)
        {
            var staffName = User.FindFirstValue("name")
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? "Unknown Staff";

            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = staffUserId,
                ActorName = staffName,
                Action = "WalkInBooking",
                Resource = "Appointment",
                ResourceId = result.Value!.AppointmentId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { result.Value.AppointmentId, result.Value.PatientId, result.Value.QueuePosition }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new patient record for walk-in or quick registration (AC-3).
    /// </summary>
    /// <param name="request">Patient creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created patient details.</returns>
    /// <response code="200">Patient created.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost("~/api/scheduling/patients")]
    [ProducesResponseType(typeof(PatientCreatedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatient(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Problem(
                detail: "First name and last name are required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var fullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}";
        DateOnly? dob = null;
        if (!string.IsNullOrWhiteSpace(request.DateOfBirth) && DateOnly.TryParse(request.DateOfBirth, out var parsedDob))
        {
            dob = parsedDob;
        }

        var patient = await _patientLookup.CreateWalkInPatientAsync(
            fullName,
            null, // email not provided in this flow
            request.ContactNumber,
            dob,
            cancellationToken);

        _logger.LogInformation("Created patient {PatientId} ({FullName}) via quick registration", patient.PatientId, fullName);

        // Audit log for patient registration
        if (_auditService != null)
        {
            var staffUserId = GetStaffUserId();
            var staffName = User.FindFirstValue("name")
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? "Unknown Staff";

            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = staffUserId,
                ActorName = staffName,
                Action = "PatientRegistration",
                Resource = "Patient",
                ResourceId = patient.PatientId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { patient.PatientId, patient.FullName }
            }, cancellationToken);
        }

        return Ok(new PatientCreatedResult(
            patient.PatientId.ToString(),
            patient.FullName,
            patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
            request.ContactNumber ?? "",
            patient.Email ?? "",
            $"MRN-{patient.PatientId.ToString()[..8].ToUpperInvariant()}"));
    }

    private Guid? GetStaffUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}

/// <summary>
/// Request body for creating a patient.
/// </summary>
public sealed record CreatePatientRequest(
    string FirstName,
    string LastName,
    string? ContactNumber,
    string? DateOfBirth);

/// <summary>
/// Response for patient creation.
/// </summary>
public sealed record PatientCreatedResult(
    string Id,
    string FullName,
    string DateOfBirth,
    string ContactNumber,
    string Email,
    string Mrn);

/// <summary>
/// Request body for walk-in booking.
/// </summary>
public sealed record WalkInBookingRequest(
    Guid? ExistingPatientId,
    string? PatientName,
    string? PatientEmail,
    string? PatientPhone,
    DateOnly? PatientDateOfBirth,
    Guid ProviderId,
    string? Reason);
