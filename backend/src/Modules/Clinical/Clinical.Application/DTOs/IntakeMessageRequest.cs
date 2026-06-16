using System.ComponentModel.DataAnnotations;

namespace Clinical.Application.DTOs;

/// <summary>
/// Request DTO for sending a patient message during intake conversation.
/// </summary>
public sealed record IntakeMessageRequest(
    [Required, StringLength(2000, MinimumLength = 1)]
    string Message);
