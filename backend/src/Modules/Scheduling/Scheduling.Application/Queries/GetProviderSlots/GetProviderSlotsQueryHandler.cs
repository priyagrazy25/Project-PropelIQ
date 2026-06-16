using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Queries.SearchProviders;
using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Application.Queries.GetProviderSlots;

public sealed class GetProviderSlotsQueryHandler
{
    private readonly ISchedulingDbContext _dbContext;

    public GetProviderSlotsQueryHandler(ISchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetProviderSlotsResult>> HandleAsync(
        GetProviderSlotsQuery query,
        CancellationToken cancellationToken = default)
    {
        var providerExists = await _dbContext.Providers
            .AsNoTracking()
            .AnyAsync(p => p.Id == query.ProviderId && p.IsActive && !p.IsDeleted, cancellationToken);

        if (!providerExists)
        {
            return Result<GetProviderSlotsResult>.Failure("Provider not found.");
        }

        var date = query.Date?.Date ?? DateTime.UtcNow.Date;
        var dateEnd = date.AddDays(1);

        var slots = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Where(s => s.ProviderId == query.ProviderId
                        && s.StartTime >= date
                        && s.StartTime < dateEnd)
            .OrderBy(s => s.StartTime)
            .Select(s => new SlotDto(
                s.Id,
                s.StartTime,
                s.StartTime.AddMinutes(s.DurationMinutes),
                s.Status == SlotStatus.Available))
            .ToListAsync(cancellationToken);

        return Result<GetProviderSlotsResult>.Success(
            new GetProviderSlotsResult(query.ProviderId, slots));
    }
}
