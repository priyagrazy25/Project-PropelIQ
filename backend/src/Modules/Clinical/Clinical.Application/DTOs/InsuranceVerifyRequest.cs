using System.ComponentModel.DataAnnotations;

namespace Clinical.Application.DTOs;

public sealed record InsuranceVerifyRequest(
    [Required, StringLength(200, MinimumLength = 1)] string InsuranceName,
    [Required, StringLength(50, MinimumLength = 1)] string MemberId);
