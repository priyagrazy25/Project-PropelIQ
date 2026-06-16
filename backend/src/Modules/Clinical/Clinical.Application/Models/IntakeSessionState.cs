using Clinical.Application.DTOs;

namespace Clinical.Application.Models;

/// <summary>
/// Session state stored in Redis with 1-hour TTL for intake conversation.
/// </summary>
public sealed class IntakeSessionState
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string Status { get; set; } = "Active";
    public List<ConversationTurn> ConversationHistory { get; set; } = [];
    public List<ParsedFieldDto> ParsedFields { get; set; } = [];
    public int TotalTokensUsed { get; set; }
    public int ConsecutiveLowConfidenceCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public sealed class ConversationTurn
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
