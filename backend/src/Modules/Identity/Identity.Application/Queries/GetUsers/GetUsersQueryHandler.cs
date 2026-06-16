using Identity.Application.Abstractions;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Identity.Application.Queries.GetUsers;

public sealed class GetUsersQueryHandler
{
    private readonly IIdentityDbContext _dbContext;

    public GetUsersQueryHandler(IIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetUsersResult>> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var usersQuery = _dbContext.Users.AsNoTracking();

        // Search by name or email
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim().ToLowerInvariant();
            usersQuery = usersQuery.Where(u =>
                u.FullName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm));
        }

        // Filter by role
        if (!string.IsNullOrWhiteSpace(query.Role) &&
            Enum.TryParse<UserRole>(query.Role, ignoreCase: true, out var role))
        {
            usersQuery = usersQuery.Where(u => u.Role == role);
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<UserStatus>(query.Status, ignoreCase: true, out var status))
        {
            usersQuery = usersQuery.Where(u => u.Status == status);
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);

        var users = await usersQuery
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Role.ToString(),
                u.Status.ToString(),
                u.ContactNumber,
                null))
            .ToListAsync(cancellationToken);

        return Result<GetUsersResult>.Success(
            new GetUsersResult(users, totalCount, page, pageSize));
    }
}
