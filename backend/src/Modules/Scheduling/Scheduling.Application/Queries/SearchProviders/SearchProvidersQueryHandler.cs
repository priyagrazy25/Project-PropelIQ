using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Scheduling.Application.Queries.SearchProviders;

public sealed class SearchProvidersQueryHandler
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public SearchProvidersQueryHandler(ISchedulingDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<Result<SearchProvidersResult>> HandleAsync(
        SearchProvidersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Build cache key from search params
        var cacheKey = BuildCacheKey(query, page, pageSize);

        var cached = await _cacheService.GetAsync<SearchProvidersResult>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<SearchProvidersResult>.Success(cached);
        }

        // Build provider query
        var providersQuery = _dbContext.Providers
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLowerInvariant();
            providersQuery = providersQuery.Where(p => p.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(query.Specialty))
        {
            var specialty = query.Specialty.Trim().ToLowerInvariant();
            providersQuery = providersQuery.Where(p => p.Specialty.ToLower().Contains(specialty));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location.Trim().ToLowerInvariant();
            providersQuery = providersQuery.Where(p => p.Location != null && p.Location.ToLower().Contains(location));
        }

        // Determine slot search window:
        // - Specific date requested → search that single day only (future-only cutoff for today)
        // - No date requested       → search from now across the next 7 days so results always appear
        var now = DateTime.UtcNow;
        DateTime startCutoff;
        DateTime dateEnd;

        if (query.Date.HasValue)
        {
            var dateFilter = query.Date.Value.Date;
            startCutoff = dateFilter == now.Date ? now : dateFilter;
            dateEnd = dateFilter.AddDays(1);
        }
        else
        {
            startCutoff = now;
            dateEnd = now.Date.AddDays(8); // next 7 full days
        }

        providersQuery = providersQuery.Where(p =>
            p.Slots.Any(s => s.Status == SlotStatus.Available && s.StartTime >= startCutoff && s.StartTime < dateEnd));

        var totalCount = await providersQuery.CountAsync(cancellationToken);

        // Apply sorting
        providersQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => providersQuery.OrderBy(p => p.Name),
            "rating" => providersQuery.OrderByDescending(p => p.Name), // Rating field not on entity; default to name
            _ => providersQuery.OrderBy(p => p.Slots
                .Where(s => s.Status == SlotStatus.Available && s.StartTime >= startCutoff && s.StartTime < dateEnd)
                .Min(s => s.StartTime)) // Earliest available
        };

        var providers = await providersQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProviderDto(
                p.Id,
                p.Name,
                p.Specialty,
                p.Location,
                4.5, // Placeholder rating — no rating field on Provider entity
                true, // Active providers are accepting patients
                p.Slots
                    .Where(s => s.Status == SlotStatus.Available && s.StartTime >= startCutoff && s.StartTime < dateEnd)
                    .OrderBy(s => s.StartTime)
                    .Take(8) // Limit slots per card
                    .Select(s => new SlotDto(
                        s.Id,
                        s.StartTime,
                        s.StartTime.AddMinutes(s.DurationMinutes),
                        s.Status == SlotStatus.Available))
                    .ToList(),
                p.Slots
                    .Where(s => s.Status == SlotStatus.Available && s.StartTime >= startCutoff)
                    .OrderBy(s => s.StartTime)
                    .Select(s => (DateTime?)s.StartTime)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var result = new SearchProvidersResult(providers, totalCount, page, pageSize);

        // Cache with L1 TTL (30s)
        await _cacheService.SetAsync(cacheKey, result, CacheTier.L1, cancellationToken);

        return Result<SearchProvidersResult>.Success(result);
    }

    private static string BuildCacheKey(SearchProvidersQuery query, int page, int pageSize)
    {
        return $"provider-search:{query.Name?.ToLowerInvariant()}:{query.Specialty?.ToLowerInvariant()}:{query.Location?.ToLowerInvariant()}:{query.Date:yyyy-MM-dd}:{page}:{pageSize}:{query.SortBy?.ToLowerInvariant()}";
    }
}
