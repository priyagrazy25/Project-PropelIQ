using Clinical.Domain.Enums;

namespace Clinical.Application.DTOs;

public sealed record InsuranceCheckResult(
    InsuranceVerificationStatus Status,
    string Message);
