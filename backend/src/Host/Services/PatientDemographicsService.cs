using Clinical.Application.Abstractions;
using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Host.Services;

/// <summary>
/// Cross-module patient demographics implementation bridging Identity and Clinical modules.
/// Used for 360-degree patient view (SCR-016).
/// </summary>
public sealed class PatientDemographicsService : IPatientDemographicsService
{
    private readonly IIdentityDbContext _identityDb;

    public PatientDemographicsService(IIdentityDbContext identityDb)
    {
        _identityDb = identityDb;
    }

    public async Task<PatientDemographicsResult?> GetByIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _identityDb.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Join(_identityDb.Users.AsNoTracking(),
                  p => p.UserId,
                  u => u.Id,
                  (p, u) => new PatientDemographicsResult(
                      p.Id,
                      u.Id,
                      u.FullName,
                      u.Email,
                      u.ContactNumber,
                      u.DateOfBirth,
                      u.Address,
                      p.InsuranceProvider,
                      p.InsurancePolicyNumber))
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}
