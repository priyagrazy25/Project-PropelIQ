using System.ComponentModel.DataAnnotations;

namespace Clinical.Application.DTOs;

/// <summary>
/// Request DTO for updating parsed fields during intake review (AC-5).
/// </summary>
public sealed record FieldUpdateRequest(
    [Required] IReadOnlyList<FieldEdit> Fields);

public sealed record FieldEdit(
    [Required, StringLength(100, MinimumLength = 1)]
    string FieldName,

    [Required, StringLength(1000)]
    string Value);
