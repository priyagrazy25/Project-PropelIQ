namespace Scheduling.Application.Queries.SearchProviders;

public sealed record SearchProvidersQuery(
    string? Name,
    string? Specialty,
    string? Location,
    DateTime? Date,
    int Page,
    int PageSize,
    string? SortBy);
