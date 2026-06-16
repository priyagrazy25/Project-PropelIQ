using System.Security.Claims;
using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedKernel.Audit;

namespace Clinical.API.Controllers;

/// <summary>
/// 360-degree patient view endpoints (SCR-016, AIR-002, NFR-004).
/// Provides aggregated clinical data with semantic de-duplication.
/// </summary>
[ApiController]
[Route("api/clinical/patients")]
[Authorize]
[Produces("application/json")]
public class PatientView360Controller : ControllerBase
{
    private readonly IPatientView360Service _patientView360Service;
    private readonly IAuditService? _auditService;
    private readonly ILogger<PatientView360Controller> _logger;

    public PatientView360Controller(
        IPatientView360Service patientView360Service,
        ILogger<PatientView360Controller> logger,
        IAuditService? auditService = null)
    {
        _patientView360Service = patientView360Service;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Gets the 360-degree view for a specific patient (AC-1, AC-5, AC-6).
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated patient view with categorized clinical data.</returns>
    /// <response code="200">360-degree view returned successfully.</response>
    /// <response code="404">Patient not found.</response>
    /// <response code="403">Not authorized to view this patient's data.</response>
    [HttpGet("{patientId:guid}/360-view")]
    [ProducesResponseType(typeof(PatientView360Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientView360(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Verify access: patients can only view their own data, staff can view any
        if (!CanAccessPatient(patientId))
        {
            return Problem(
                detail: "You are not authorized to view this patient's data.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied");
        }

        var result = await _patientView360Service.GetPatientView360Async(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        // Audit log for PHI access (HIPAA compliance)
        if (_auditService != null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue("name")
                           ?? User.FindFirstValue(ClaimTypes.Name)
                           ?? "Unknown";

            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = Guid.TryParse(userId, out var uid) ? uid : null,
                ActorName = userName,
                Action = "AccessPatientPHI",
                Resource = "PatientView360",
                ResourceId = patientId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { PatientId = patientId, AccessType = "360View" }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the 360-degree view for the current authenticated patient (AC-1, AC-5, AC-6).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated patient view with categorized clinical data.</returns>
    /// <response code="200">360-degree view returned successfully.</response>
    /// <response code="404">Patient profile not found.</response>
    /// <response code="400">Unable to identify patient.</response>
    [HttpGet("me/360-view")]
    [ProducesResponseType(typeof(PatientView360Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrentPatientView360(CancellationToken cancellationToken = default)
    {
        var patientIdClaim = User.FindFirstValue("PatientId");
        if (!Guid.TryParse(patientIdClaim, out var patientId))
        {
            return Problem(
                detail: "Unable to identify patient from authentication token.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Authentication Error");
        }

        var result = await _patientView360Service.GetPatientView360Async(patientId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        // Audit log for PHI access (HIPAA compliance)
        if (_auditService != null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue("name")
                           ?? User.FindFirstValue(ClaimTypes.Name)
                           ?? "Unknown";

            await _auditService.LogActionAsync(new AuditEntry
            {
                ActorId = Guid.TryParse(userId, out var uid) ? uid : null,
                ActorName = userName,
                Action = "AccessPatientPHI",
                Resource = "PatientView360",
                ResourceId = patientId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CorrelationId = HttpContext.TraceIdentifier,
                AfterState = new { PatientId = patientId, AccessType = "SelfView" }
            }, cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verifies if the current user can access the specified patient's data.
    /// Patients can only view their own data. Staff (Provider, FrontDesk, Admin) can view any.
    /// </summary>
    private bool CanAccessPatient(Guid patientId)
    {
        // Staff roles can access any patient
        if (User.IsInRole("Provider") || User.IsInRole("FrontDesk") || User.IsInRole("Admin"))
        {
            return true;
        }

        // For patients, verify they're accessing their own data
        var patientIdClaim = User.FindFirstValue("PatientId");
        return Guid.TryParse(patientIdClaim, out var currentPatientId) && currentPatientId == patientId;
    }
}
