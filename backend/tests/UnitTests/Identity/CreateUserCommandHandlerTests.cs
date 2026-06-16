using Identity.Application.Abstractions;
using Identity.Application.Commands.CreateUser;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public sealed class CreateUserCommandHandlerTests
{
    private static CreateUserCommand ValidCommand() => new(
        FirstName: "John",
        LastName: "Doe",
        Email: "john.doe@example.com",
        Role: "Admin",
        Phone: "(555) 123-4567");

    private static (CreateUserCommandHandler handler, Mock<IIdentityDbContext> dbMock, Mock<IPasswordHasher> hasherMock)
        CreateHandler(List<User>? existingUsers = null)
    {
        existingUsers ??= [];
        var queryable = existingUsers.AsQueryable();
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

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_temp_password");

        var validator = new CreateUserCommandValidator();
        var logger = new Mock<ILogger<CreateUserCommandHandler>>();
        var handler = new CreateUserCommandHandler(dbMock.Object, hasherMock.Object, validator, logger.Object);

        return (handler, dbMock, hasherMock);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesUserSuccessfully()
    {
        var (handler, dbMock, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.UserId);
        dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsConflict()
    {
        var existing = new User { Email = "john.doe@example.com" };
        var (handler, _, _) = CreateHandler([existing]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("DUPLICATE_EMAIL", result.Error);
    }

    [Fact]
    public async Task HandleAsync_InvalidRole_ReturnsFailure()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { Role = "SuperAdmin" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_EmptyFirstName_ReturnsValidationError()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { FirstName = "" });

        Assert.False(result.IsSuccess);
        Assert.Contains("First name is required", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ReturnsValidationError()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { Email = "not-an-email" });

        Assert.False(result.IsSuccess);
        Assert.Contains("valid email", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_NormalizesEmail()
    {
        var (handler, dbMock, _) = CreateHandler();
        User? captured = null;
        dbMock.Setup(d => d.Users).Returns(() =>
        {
            var empty = new List<User>().AsQueryable();
            var mockSet = new Mock<DbSet<User>>();
            mockSet.As<IQueryable<User>>().Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<User>(empty.Provider));
            mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(empty.Expression);
            mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(empty.ElementType);
            mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(empty.GetEnumerator());
            mockSet.As<IAsyncEnumerable<User>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<User>(empty.GetEnumerator()));
            mockSet.Setup(m => m.Add(It.IsAny<User>())).Callback<User>(u => captured = u);
            return mockSet.Object;
        });

        await handler.HandleAsync(ValidCommand() with { Email = "  John.Doe@EXAMPLE.COM  " });

        Assert.NotNull(captured);
        Assert.Equal("john.doe@example.com", captured!.Email);
    }
}
