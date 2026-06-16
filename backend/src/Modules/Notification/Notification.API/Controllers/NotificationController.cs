using Microsoft.AspNetCore.Mvc;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Module = "Notification", Status = "Active" });
    }
}
