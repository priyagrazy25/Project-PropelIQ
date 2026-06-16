using Identity.Application.Abstractions;
using Identity.Application.Queries.GetUsers;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests.Identity;

public sealed class GetUsersQueryHandlerTests
{
    private static (GetUsersQueryHandler handler, Mock<IIdentityDbContext> dbMock)
        CreateHandler(List<User>? users = null)
    {
        users ??= [];
        var dbMock = MockDbContext(users);
        var handler = new GetUsersQueryHandler(dbMock.Object);
        return (handler, dbMock);
    }

    private static Mock<IIdentityDbContext> MockDbContext(List<User> users)
    {
        var queryable = users.AsQueryable();
        var mockSet = new Mock<DbSet<User>>();
        mockSet.As<IQueryable<User>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<User>(queryable.Provider));
        mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mockSet.As<IAsyncEnumerable<User>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<User>(queryable.GetEnumerator()));

        var dbMock = new Mock<IIdentityDbContext>();
        dbMock.Setup(d => d.Users).Returns(mockSet.Object);
        return dbMock;
    }

    private static List<User> SeedUsers() =>
    [
        new User { FullName = "Alice Admin", Email = "alice@test.com", Role = UserRole.Admin, Status = UserStatus.Active },
        new User { FullName = "Bob Patient", Email = "bob@test.com", Role = UserRole.Patient, Status = UserStatus.Active },
        new User { FullName = "Carol Provider", Email = "carol@test.com", Role = UserRole.Provider, Status = UserStatus.Inactive },
    ];

    [Fact]
    public async Task HandleAsync_ReturnsAllUsers_WhenNoFilters()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 10, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalCount);
        Assert.Equal(3, result.Value.Users.Count);
    }

    [Fact]
    public async Task HandleAsync_SearchByName_FiltersCorrectly()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 10, "alice", null, null));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Users);
        Assert.Equal("Alice Admin", result.Value.Users[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_SearchByEmail_FiltersCorrectly()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 10, "bob@test", null, null));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Users);
        Assert.Equal("bob@test.com", result.Value.Users[0].Email);
    }

    [Fact]
    public async Task HandleAsync_FilterByRole_ReturnsMatchingUsers()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 10, null, "Admin", null));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Users);
        Assert.Equal("Admin", result.Value.Users[0].Role);
    }

    [Fact]
    public async Task HandleAsync_FilterByStatus_ReturnsMatchingUsers()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 10, null, null, "Inactive"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Users);
        Assert.Equal("Carol Provider", result.Value.Users[0].FullName);
    }

    [Fact]
    public async Task HandleAsync_Pagination_RespectsPageSize()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 2, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Users.Count);
    }

    [Fact]
    public async Task HandleAsync_EmptyList_ReturnsEmptyResult()
    {
        var (handler, _) = CreateHandler([]);

        var result = await handler.HandleAsync(new GetUsersQuery(1, 10, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalCount);
        Assert.Empty(result.Value.Users);
    }

    [Fact]
    public async Task HandleAsync_ClampPageSize_LimitsTo100()
    {
        var (handler, _) = CreateHandler(SeedUsers());

        var result = await handler.HandleAsync(new GetUsersQuery(1, 999, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.PageSize);
    }
}
