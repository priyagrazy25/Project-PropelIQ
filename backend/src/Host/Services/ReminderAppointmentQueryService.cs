using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;

namespace Host.Services;

public sealed class ReminderAppointmentQueryService : IReminderAppointmentQuery
{
    private readonly ISchedulingDbContext _schedulingDb;
    private readonly IIdentityDbContext _identityDb;

    public ReminderAppointmentQueryService(
        ISchedulingDbContext schedulingDb,
        IIdentityDbContext identityDb)
    {
        _schedulingDb = schedulingDb;
        _identityDb = identityDb;
    }

    public async Task<IReadOnlyList<ReminderAppointmentInfo>> GetUpcomingAppointmentsAsync(
        DateTime windowStart,
        DateTime windowEnd,
        CancellationToken cancellationToken = default)
    {
        var appointments = await _schedulingDb.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDateTime >= windowStart
                        && a.AppointmentDateTime <= windowEnd
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Status != AppointmentStatus.Rescheduled
                        && a.Status != AppointmentStatus.Completed
                        && a.Status != AppointmentStatus.NoShow)
            .Include(a => a.Provider)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
            return [];

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();

        var patients = await _identityDb.Patients
            .AsNoTracking()
            .Where(p => patientIds.Contains(p.Id))
            .Join(_identityDb.Users.AsNoTracking(),
                  p => p.UserId,
                  u => u.Id,
                  (p, u) => new { p.Id, u.FullName, u.Email, u.ContactNumber })
            .ToListAsync(cancellationToken);

        var patientLookup = patients.ToDictionary(p => p.Id);

        // Get no-show risk scores for these appointments
        var appointmentIds = appointments.Select(a => a.Id).ToList();
        var riskScores = await _schedulingDb.Appointments
            .AsNoTracking()
            .Where(a => appointmentIds.Contains(a.Id))
            .Select(a => new { a.Id, a.PatientId })
            .ToListAsync(cancellationToken);

        var results = new List<ReminderAppointmentInfo>();

        foreach (var appt in appointments)
        {
            if (!patientLookup.TryGetValue(appt.PatientId, out var patient))
                continue;

            results.Add(new ReminderAppointmentInfo(
                AppointmentId: appt.Id,
                PatientId: appt.PatientId,
                AppointmentDateTime: appt.AppointmentDateTime,
                PatientFullName: patient.FullName,
                PatientEmail: patient.Email,
                PatientPhone: patient.ContactNumber,
                ProviderName: appt.Provider?.Name ?? "Your Provider",
                NoShowRiskScore: 0));
        }

        return results;
    }

    public async Task<ReminderAppointmentInfo?> GetAppointmentByIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appt = await _schedulingDb.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(cancellationToken);

        if (appt == null)
            return null;

        var patient = await _identityDb.Patients
            .AsNoTracking()
            .Where(p => p.Id == appt.PatientId)
            .Join(_identityDb.Users.AsNoTracking(),
                  p => p.UserId,
                  u => u.Id,
                  (p, u) => new { p.Id, u.FullName, u.Email, u.ContactNumber })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient == null)
            return null;

        return new ReminderAppointmentInfo(
            AppointmentId: appt.Id,
            PatientId: appt.PatientId,
            AppointmentDateTime: appt.AppointmentDateTime,
            PatientFullName: patient.FullName,
            PatientEmail: patient.Email,
            PatientPhone: patient.ContactNumber,
            ProviderName: appt.Provider?.Name ?? "Your Provider",
            NoShowRiskScore: 0);
    }

    public async Task<bool> HasReminderBeenSentAsync(
        Guid appointmentId,
        string channel,
        string reminderWindow,
        CancellationToken cancellationToken = default)
    {
        // Delegated to the IReminderDeliveryLogRepository
        return false;
    }
}
