using Clinical.Application.DTOs;

namespace Clinical.Application.Models;

/// <summary>
/// State stored in Redis for tracking mode switches between AI and manual intake.
/// </summary>
public sealed class ModeSwitchState
{
    public Guid AppointmentId { get; set; }
    public string CurrentMode { get; set; } = "AI";
    public Guid? AiSessionId { get; set; }
    public List<ParsedFieldDto>? AiParsedFields { get; set; }
    public ManualIntakeResponse? ManualSnapshot { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? LastSwitchedAt { get; set; }
}
