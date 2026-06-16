using SharedKernel.Domain;

namespace Identity.Domain.Entities;

public sealed class InsurancePlan : BaseEntity
{
    public string InsuranceName { get; set; } = string.Empty;
    public string ValidMemberIdPattern { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
