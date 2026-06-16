using Clinical.Application.DTOs;
using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

public interface IInsuranceValidationService
{
    Task<Result<InsuranceCheckResult>> ValidateAsync(
        InsuranceCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<InsuranceCheckResult>> GetStatusAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<InsuranceVerifyResponse> VerifyAsync(
        InsuranceVerifyRequest request,
        CancellationToken cancellationToken = default);
}
