using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Identity module endpoints for authentication, authorization, and user management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IdentityController : ControllerBase
{
    /// <summary>
    /// Returns the current status of the Identity module.
    /// </summary>
    /// <returns>Module name and operational status.</returns>
    /// <response code="200">Module is active and operational.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ModuleStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new ModuleStatusResponse("Identity", "Active"));
    }
}

/// <summary>Module status response.</summary>
/// <param name="Module">Module name.</param>
/// <param name="Status">Operational status.</param>
public record ModuleStatusResponse(string Module, string Status);
