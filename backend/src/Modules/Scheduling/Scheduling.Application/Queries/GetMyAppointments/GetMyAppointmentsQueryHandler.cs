using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using SharedKernel.Domain;

namespace Scheduling.Application.Queries.GetMyAppointments;

public sealed class GetMyAppointmentsQueryHandler
{
    private readonly ISchedulingDbContext _dbContext;

    public GetMyAppointmentsQueryHandler(ISchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<MyAppointmentResult>>> HandleAsync(
        GetMyAppointmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.PatientId == query.PatientId && !a.IsDeleted)
            .OrderByDescending(a => a.AppointmentDateTime)
            .Select(a => new MyAppointmentResult(
                a.Id,
                a.ProviderId,
                a.Provider.Name,
                a.Provider.Specialty,
                a.Provider.Location ?? "",
                a.AppointmentDateTime,
                a.DurationMinutes,
                a.Status.ToString(),
                a.Type.ToString()))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<MyAppointmentResult>>.Success(appointments);
    }
}
