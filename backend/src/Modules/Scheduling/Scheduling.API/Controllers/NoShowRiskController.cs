using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.API.Controllers;

/// <summary>
/// No-show risk prediction and model management endpoints (AIR-007, AIR-O03).
/// </summary>
[ApiController]
[Route("api/scheduling/risk")]
[Authorize(Roles = "Admin,Provider,FrontDesk")]
[Produces("application/json")]
public class NoShowRiskController : ControllerBase
{
    private readonly INoShowRiskService _riskService;
    private readonly ISchedulingDbContext _dbContext;
    private readonly ILogger<NoShowRiskController> _logger;

    public NoShowRiskController(
        INoShowRiskService riskService,
        ISchedulingDbContext dbContext,
        ILogger<NoShowRiskController> logger)
    {
        _riskService = riskService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Predicts no-show risk for a single appointment.
    /// </summary>
    /// <param name="request">Risk prediction input features.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Risk score (0-100) with level and contributing factors.</returns>
    [HttpPost("predict")]
    [ProducesResponseType(typeof(NoShowRiskPrediction), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PredictRisk(
        [FromBody] PredictRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            return Problem(
                detail: "AppointmentId is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var input = new NoShowRiskInput(
            PatientId: request.PatientId,
            AppointmentId: request.AppointmentId,
            AppointmentType: request.AppointmentType,
            DayOfWeek: request.DayOfWeek,
            HourOfDay: request.HourOfDay,
            PriorNoShows: request.PriorNoShows,
            PriorCancellations: request.PriorCancellations,
            TotalPriorAppointments: request.TotalPriorAppointments,
            DaysSinceLastVisit: request.DaysSinceLastVisit,
            HasInsurance: request.HasInsurance,
            IsNewPatient: request.IsNewPatient,
            LeadTimeDays: request.LeadTimeDays);

        var prediction = await _riskService.PredictRiskAsync(input, cancellationToken);
        return Ok(prediction);
    }

    /// <summary>
    /// Gets risk assessments for all scheduled appointments in a date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of appointment risk assessments sorted by risk score (descending).</returns>
    [HttpGet("assessments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentRiskAssessment>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRiskAssessments(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
        {
            return Problem(
                detail: "Start date must be before or equal to end date.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        if ((endDate - startDate).TotalDays > 30)
        {
            return Problem(
                detail: "Date range cannot exceed 30 days.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var assessments = await _riskService.GetRiskAssessmentsAsync(
            startDate, endDate, cancellationToken);

        return Ok(assessments);
    }

    /// <summary>
    /// Gets all available model versions.
    /// </summary>
    /// <returns>List of model versions with metadata.</returns>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(ModelVersionsResponse), StatusCodes.Status200OK)]
    public IActionResult GetModelVersions()
    {
        var versions = _riskService.GetAvailableVersions();
        var activeVersion = _riskService.GetActiveModelVersion();

        return Ok(new ModelVersionsResponse(
            ActiveVersion: activeVersion,
            Versions: versions));
    }

    /// <summary>
    /// Rolls back to a previous model version (AIR-O03: must be within 15 minutes of deployment).
    /// </summary>
    /// <param name="request">Rollback request with target version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure with message.</returns>
    [HttpPost("rollback")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RollbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RollbackModel(
        [FromBody] RollbackRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TargetVersion))
        {
            return Problem(
                detail: "Target version is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var success = await _riskService.RollbackToVersionAsync(
            request.TargetVersion, cancellationToken);

        if (!success)
        {
            return Problem(
                detail: "Rollback failed. Either the version doesn't exist or the 15-minute rollback window has expired.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rollback Failed");
        }

        _logger.LogInformation("Model rolled back to version {Version}", request.TargetVersion);

        return Ok(new RollbackResponse(
            Success: true,
            NewActiveVersion: _riskService.GetActiveModelVersion(),
            Message: $"Successfully rolled back to version {request.TargetVersion}"));
    }

    /// <summary>
    /// Triggers model retraining with current data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New model version information.</returns>
    [HttpPost("retrain")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RetrainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RetrainModel(CancellationToken cancellationToken = default)
    {
        try
        {
            var newVersion = await _riskService.RetrainModelAsync(cancellationToken);

            _logger.LogInformation("Model retrained, new version: {Version}", newVersion);

            return Ok(new RetrainResponse(
                Success: true,
                NewVersion: newVersion,
                Message: "Model retrained and activated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model retraining failed");
            return Problem(
                detail: "An error occurred during model retraining.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Retraining Failed");
        }
    }

    /// <summary>
    /// Seeds test scheduled appointments for risk assessment testing (DEV ONLY).
    /// </summary>
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SeedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedTestAppointments(CancellationToken cancellationToken = default)
    {
        // Get first 3 providers
        var providers = await _dbContext.Providers
            .Where(p => p.IsActive && !p.IsDeleted)
            .Take(3)
            .ToListAsync(cancellationToken);

        if (!providers.Any())
        {
            return Problem(
                detail: "No providers found to seed appointments.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Seed Error");
        }

        // Get patient IDs from existing appointments
        var patientIds = await _dbContext.Appointments
            .Select(a => a.PatientId)
            .Distinct()
            .Take(3)
            .ToListAsync(cancellationToken);

        if (!patientIds.Any())
        {
            return Problem(
                detail: "No patients found to seed appointments.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Seed Error");
        }

        var tomorrow = DateTime.Today.AddDays(1);
        var nextWeek = DateTime.Today.AddDays(7);
        var appointments = new List<Appointment>();
        var random = new Random();

        // Create appointments for tomorrow
        for (int i = 0; i < 5; i++)
        {
            var provider = providers[random.Next(providers.Count)];
            var patientId = patientIds[random.Next(patientIds.Count)];
            var hour = 8 + i * 2; // 8, 10, 12, 14, 16

            appointments.Add(new Appointment
            {
                PatientId = patientId,
                ProviderId = provider.Id,
                AppointmentDateTime = tomorrow.AddHours(hour),
                DurationMinutes = 30,
                Type = AppointmentType.InPerson,
                Status = AppointmentStatus.Scheduled,
                Reason = "Follow-up visit"
            });
        }

        // Create appointments for next week
        for (int i = 0; i < 3; i++)
        {
            var provider = providers[random.Next(providers.Count)];
            var patientId = patientIds[random.Next(patientIds.Count)];
            var hour = 9 + i * 3; // 9, 12, 15

            appointments.Add(new Appointment
            {
                PatientId = patientId,
                ProviderId = provider.Id,
                AppointmentDateTime = nextWeek.AddHours(hour),
                DurationMinutes = 30,
                Type = AppointmentType.InPerson,
                Status = AppointmentStatus.Scheduled,
                Reason = "Consultation"
            });
        }

        _dbContext.Appointments.AddRange(appointments);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} test scheduled appointments", appointments.Count);

        return Ok(new SeedResponse(
            Success: true,
            Count: appointments.Count,
            Message: $"Seeded {appointments.Count} scheduled appointments for tomorrow and next week."));
    }
}

#region Request/Response DTOs

/// <summary>
/// Request for predicting no-show risk.
/// </summary>
public record PredictRiskRequest(
    Guid PatientId,
    Guid AppointmentId,
    int AppointmentType,
    int DayOfWeek,
    int HourOfDay,
    int PriorNoShows,
    int PriorCancellations,
    int TotalPriorAppointments,
    int DaysSinceLastVisit,
    bool HasInsurance,
    bool IsNewPatient,
    int LeadTimeDays);

/// <summary>
/// Response containing model versions.
/// </summary>
public record ModelVersionsResponse(
    string ActiveVersion,
    IReadOnlyList<ModelVersionInfo> Versions);

/// <summary>
/// Request for model rollback.
/// </summary>
public record RollbackRequest(string TargetVersion);

/// <summary>
/// Response from model rollback.
/// </summary>
public record RollbackResponse(
    bool Success,
    string NewActiveVersion,
    string Message);

/// <summary>
/// Response from model retraining.
/// </summary>
public record RetrainResponse(
    bool Success,
    string NewVersion,
    string Message);

/// <summary>
/// Response from seed test data operation.
/// </summary>
public record SeedResponse(
    bool Success,
    int Count,
    string Message);

#endregion
