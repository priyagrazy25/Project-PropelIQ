using Clinical.Domain.Enums;
using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class InsuranceVerification : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public string InsuranceName { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public InsuranceVerificationStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}
