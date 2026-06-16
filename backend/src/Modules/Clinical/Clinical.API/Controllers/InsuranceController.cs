using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Clinical.API.Controllers;

[ApiController]
[Route("api/clinical/insurance")]
[Authorize]
[Produces("application/json")]
public class InsuranceController : ControllerBase
{
    private readonly IInsuranceValidationService _validationService;
    private readonly ILogger<InsuranceController> _logger;

    public InsuranceController(
        IInsuranceValidationService validationService,
        ILogger<InsuranceController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Validates insurance name and member ID against seeded records (AC-1 through AC-4).
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(InsuranceCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate(
        [FromBody] InsuranceCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            return Problem(
                detail: "AppointmentId is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var result = await _validationService.ValidateAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Failed");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Pre-check insurance coverage by name and member ID without appointment context (FR-018, UC-013).
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(InsuranceVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify(
        [FromBody] InsuranceVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _validationService.VerifyAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Returns insurance verification status for a given appointment (AC-5).
    /// </summary>
    [HttpGet("{appointmentId:guid}/status")]
    [ProducesResponseType(typeof(InsuranceCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var result = await _validationService.GetStatusAsync(appointmentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(result.Value);
    }
}
