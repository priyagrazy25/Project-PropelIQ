using Identity.Application.Abstractions;
using Identity.Application.Commands.ReactivateUser;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public sealed class ReactivateUserCommandHandlerTests
{
    private static readonly Guid TargetId = Guid.NewGuid();

    private static (ReactivateUserCommandHandler handler, Mock<IIdentityDbContext> dbMock)
        CreateHandler(List<User>? users = null)
    {
        users ??= [];
        var queryable = users.AsQueryable();
        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<User>(queryable.Provider));
        mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mockUserSet.As<IAsyncEnumerable<User>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<User>(queryable.GetEnumerator()));

        var dbMock = new Mock<IIdentityDbContext>();
        dbMock.Setup(d => d.Users).Returns(mockUserSet.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var logger = new Mock<ILogger<ReactivateUserCommandHandler>>();
        var handler = new ReactivateUserCommandHandler(dbMock.Object, logger.Object);

        return (handler, dbMock);
    }

    [Fact]
    public async Task HandleAsync_InactiveUser_ReactivatesSuccessfully()
    {
        var user = new User
        {
            FullName = "Inactive User",
            Email = "inactive@test.com",
            Status = UserStatus.Inactive,
        };
        typeof(global::SharedKernel.Domain.BaseEntity).GetProperty("Id")!.SetValue(user, TargetId);

        var (handler, dbMock) = CreateHandler([user]);

        var result = await handler.HandleAsync(new ReactivateUserCommand(TargetId));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Active, user.Status);
        dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsFailure()
    {
        var (handler, _) = CreateHandler([]);

        var result = await handler.HandleAsync(new ReactivateUserCommand(TargetId));

        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task HandleAsync_AlreadyActive_ReturnsFailure()
    {
        var user = new User
        {
            FullName = "Active User",
            Email = "active@test.com",
            Status = UserStatus.Active,
        };
        typeof(global::SharedKernel.Domain.BaseEntity).GetProperty("Id")!.SetValue(user, TargetId);

        var (handler, _) = CreateHandler([user]);

        var result = await handler.HandleAsync(new ReactivateUserCommand(TargetId));

        Assert.False(result.IsSuccess);
        Assert.Equal("ALREADY_ACTIVE", result.Error);
    }
}
