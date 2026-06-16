namespace Identity.Application.Queries.GetUsers;

public sealed record GetUsersQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Role,
    string? Status);
