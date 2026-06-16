using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.WalkInBooking;

namespace Host.Services;

/// <summary>
/// Cross-module patient lookup implementation bridging Identity and Scheduling modules.
/// Used for walk-in booking patient search and inline creation (AC-2, AC-3).
/// </summary>
public sealed class PatientLookupService : IPatientLookupService
{
    private readonly IIdentityDbContext _identityDb;

    public PatientLookupService(IIdentityDbContext identityDb)
    {
        _identityDb = identityDb;
    }

    public async Task<IReadOnlyList<PatientSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.ToLowerInvariant();

        var results = await _identityDb.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Patient
                        && u.Status == UserStatus.Active
                        && (u.FullName.ToLower().Contains(normalizedQuery)
                            || u.Email.ToLower().Contains(normalizedQuery)))
            .Join(_identityDb.Patients.AsNoTracking(),
                  u => u.Id,
                  p => p.UserId,
                  (u, p) => new PatientSearchResult(
                      p.Id,
                      u.Id,
                      u.FullName,
                      u.Email,
                      u.ContactNumber,
                      u.DateOfBirth))
            .Take(20)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<PatientSearchResult?> GetByIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _identityDb.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Join(_identityDb.Users.AsNoTracking(),
                  p => p.UserId,
                  u => u.Id,
                  (p, u) => new PatientSearchResult(
                      p.Id,
                      u.Id,
                      u.FullName,
                      u.Email,
                      u.ContactNumber,
                      u.DateOfBirth))
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<PatientSearchResult> CreateWalkInPatientAsync(
        string fullName,
        string? email,
        string? phone,
        DateOnly? dateOfBirth,
        CancellationToken cancellationToken = default)
    {
        // Create minimal user record for walk-in patient (no full account, no password)
        var user = new User
        {
            Email = email?.Trim().ToLowerInvariant() ?? $"walkin-{Guid.NewGuid():N}@placeholder.local",
            PasswordHash = string.Empty,
            FullName = fullName.Trim(),
            DateOfBirth = dateOfBirth,
            ContactNumber = phone?.Trim(),
            Role = UserRole.Patient,
            Status = UserStatus.Active,
        };

        var patient = new Patient
        {
            UserId = user.Id,
        };

        _identityDb.Users.Add(user);
        _identityDb.Patients.Add(patient);
        await _identityDb.SaveChangesAsync(cancellationToken);

        return new PatientSearchResult(
            patient.Id,
            user.Id,
            user.FullName,
            user.Email,
            user.ContactNumber,
            user.DateOfBirth);
    }
}
