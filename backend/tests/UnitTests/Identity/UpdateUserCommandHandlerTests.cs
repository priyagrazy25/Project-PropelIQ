using Identity.Application.Abstractions;
using Identity.Application.Commands.UpdateUser;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public sealed class UpdateUserCommandHandlerTests
{
    private static readonly Guid ExistingUserId = Guid.NewGuid();

    private static UpdateUserCommand ValidCommand() => new(
        UserId: ExistingUserId,
        FirstName: "Jane",
        LastName: "Updated",
        Email: "jane.updated@example.com",
        Role: "Provider",
        Phone: "(555) 000-1111");

    private static User ExistingUser() => new()
    {
        Email = "jane.original@example.com",
        FullName = "Jane Original",
        Role = UserRole.Patient,
        Status = UserStatus.Active,
    };

    private static (UpdateUserCommandHandler handler, Mock<IIdentityDbContext> dbMock)
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

        var validator = new UpdateUserCommandValidator();
        var logger = new Mock<ILogger<UpdateUserCommandHandler>>();
        var handler = new UpdateUserCommandHandler(dbMock.Object, validator, logger.Object);

        return (handler, dbMock);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesUser()
    {
        var user = ExistingUser();
        // Set the Id via reflection since it's protected set
        typeof(global::SharedKernel.Domain.BaseEntity).GetProperty("Id")!.SetValue(user, ExistingUserId);

        var (handler, dbMock) = CreateHandler([user]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Updated", user.FullName);
        Assert.Equal("jane.updated@example.com", user.Email);
        Assert.Equal(UserRole.Provider, user.Role);
        dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsFailure()
    {
        var (handler, _) = CreateHandler([]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsConflict()
    {
        var user = ExistingUser();
        typeof(global::SharedKernel.Domain.BaseEntity).GetProperty("Id")!.SetValue(user, ExistingUserId);

        var otherUser = new User { Email = "jane.updated@example.com", FullName = "Other" };

        var (handler, _) = CreateHandler([user, otherUser]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("DUPLICATE_EMAIL", result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyName_ReturnsValidationError()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { FirstName = "" });

        Assert.False(result.IsSuccess);
        Assert.Contains("First name is required", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_InvalidRole_ReturnsValidationError()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { Role = "InvalidRole" });

        Assert.False(result.IsSuccess);
    }
}
