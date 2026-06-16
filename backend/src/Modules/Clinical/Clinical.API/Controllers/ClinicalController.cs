using Microsoft.AspNetCore.Mvc;

namespace Clinical.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicalController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Module = "Clinical", Status = "Active" });
    }
}
