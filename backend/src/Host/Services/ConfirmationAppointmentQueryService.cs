using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Scheduling.Application.Abstractions;

namespace Host.Services;

public sealed class ConfirmationAppointmentQueryService : IConfirmationAppointmentQuery
{
    private readonly ISchedulingDbContext _schedulingDb;
    private readonly IIdentityDbContext _identityDb;

    public ConfirmationAppointmentQueryService(
        ISchedulingDbContext schedulingDb,
        IIdentityDbContext identityDb)
    {
        _schedulingDb = schedulingDb;
        _identityDb = identityDb;
    }

    public async Task<AppointmentConfirmationInfo?> GetAppointmentForConfirmationAsync(
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
            .Where(p => p.UserId == appointment.PatientId)
            .Join(_identityDb.Users.AsNoTracking(),
                  p => p.UserId,
                  u => u.Id,
                  (p, u) => new { u.FullName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
            return null;

        return new AppointmentConfirmationInfo(
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId,
            PatientFullName: patient.FullName,
            PatientEmail: patient.Email,
            ProviderName: appointment.Provider?.Name ?? "Your Provider",
            ProviderSpecialty: appointment.Provider?.Specialty,
            AppointmentDateTime: appointment.AppointmentDateTime,
            DurationMinutes: appointment.DurationMinutes,
            AppointmentType: appointment.Type.ToString(),
            Location: appointment.Provider?.Location,
            PrepNotes: appointment.Notes);
    }
}
