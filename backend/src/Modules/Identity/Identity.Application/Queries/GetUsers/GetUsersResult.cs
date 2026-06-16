namespace Identity.Application.Queries.GetUsers;

public sealed record GetUsersResult(
    List<AdminUserDto> Users,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Status,
    string? Phone,
    string? LastLogin);
