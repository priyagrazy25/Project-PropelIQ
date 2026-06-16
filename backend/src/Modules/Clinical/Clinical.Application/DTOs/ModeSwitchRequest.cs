using System.ComponentModel.DataAnnotations;

namespace Clinical.Application.DTOs;

/// <summary>
/// Request DTO for switching intake mode between AI and manual (AC-1, AC-2).
/// </summary>
public sealed record ModeSwitchRequest(
    [Required]
    string TargetMode,

    Guid? SessionId);
