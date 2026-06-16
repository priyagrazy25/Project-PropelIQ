using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;

namespace Host.Services;

/// <summary>
/// Bridges the Notification module's calendar sync with Scheduling/Identity data.
/// Provides appointment details needed for creating external calendar events.
/// </summary>
public sealed class CalendarAppointmentQueryService : ICalendarAppointmentQuery
{
    private readonly ISchedulingDbContext _schedulingDb;
    private readonly IIdentityDbContext _identityDb;

    public CalendarAppointmentQueryService(
        ISchedulingDbContext schedulingDb,
        IIdentityDbContext identityDb)
    {
        _schedulingDb = schedulingDb;
        _identityDb = identityDb;
    }

    public async Task<CalendarAppointmentInfo?> GetAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _schedulingDb.Appointments
            .AsNoTracking()
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
            return null;

        var patient = await _identityDb.Patients
            .AsNoTracking()
            .Where(p => p.Id == appointment.PatientId)
            .Join(_identityDb.Users.AsNoTracking(),
                  p => p.UserId,
                  u => u.Id,
                  (p, u) => new { p.Id, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);

        return new CalendarAppointmentInfo(
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId,
            PatientName: patient?.FullName ?? "Patient",
            ProviderName: appointment.Provider?.Name ?? "Provider",
            StartTime: appointment.AppointmentDateTime,
            EndTime: appointment.AppointmentDateTime.AddMinutes(appointment.DurationMinutes),
            Location: appointment.Provider?.Location,
            Notes: appointment.Notes);
    }
}
