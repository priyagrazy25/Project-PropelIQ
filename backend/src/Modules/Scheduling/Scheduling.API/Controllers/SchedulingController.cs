using Microsoft.AspNetCore.Mvc;

namespace Scheduling.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulingController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Module = "Scheduling", Status = "Active" });
    }
}
