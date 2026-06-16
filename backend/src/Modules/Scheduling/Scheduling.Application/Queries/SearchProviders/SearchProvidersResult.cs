namespace Scheduling.Application.Queries.SearchProviders;

public sealed record SearchProvidersResult(
    List<ProviderDto> Providers,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ProviderDto(
    Guid Id,
    string FullName,
    string Specialty,
    string? Location,
    double Rating,
    bool IsAcceptingPatients,
    List<SlotDto> AvailableSlots,
    DateTime? NextAvailableDate);

public sealed record SlotDto(
    Guid Id,
    DateTime StartTime,
    DateTime EndTime,
    bool IsAvailable);
