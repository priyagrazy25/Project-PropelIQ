using Identity.Application.Abstractions;
using Identity.Application.Commands.Login;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests.Identity;

public sealed class LoginCommandHandlerTests
{
    private static LoginCommand ValidCommand() => new("jane@example.com", "Str0ng!Pass", "device-1");

    private static User ActiveUser() => new()
    {
        Email = "jane@example.com",
        PasswordHash = "hashed",
        FullName = "Jane Smith",
        Role = UserRole.Patient,
        Status = UserStatus.Active,
    };

    private static (LoginCommandHandler handler, Mock<IIdentityDbContext> dbMock,
        Mock<IPasswordHasher> hasherMock, Mock<IJwtTokenService> jwtMock)
        CreateHandler(List<User>? users = null)
    {
        users ??= [];

        var usersQueryable = users.AsQueryable();
        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<User>(usersQueryable.Provider));
        mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(usersQueryable.Expression);
        mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(usersQueryable.ElementType);
        mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(usersQueryable.GetEnumerator());
        mockUserSet.As<IAsyncEnumerable<User>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<User>(usersQueryable.GetEnumerator()));

        var mockRefreshSet = new Mock<DbSet<RefreshToken>>();

        var dbMock = new Mock<IIdentityDbContext>();
        dbMock.Setup(d => d.Users).Returns(mockUserSet.Object);
        dbMock.Setup(d => d.RefreshTokens).Returns(mockRefreshSet.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .Returns("test-access-token");
        jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("test-refresh-token");

        var handler = new LoginCommandHandler(dbMock.Object, hasherMock.Object, jwtMock.Object);
        return (handler, dbMock, hasherMock, jwtMock);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsSuccess()
    {
        var (handler, _, _, jwtMock) = CreateHandler([ActiveUser()]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal("test-access-token", result.Value!.AccessToken);
        Assert.Equal("test-refresh-token", result.Value.RefreshToken);
        Assert.Equal("Patient", result.Value.Role);
        Assert.Equal("Jane Smith", result.Value.FullName);
        jwtMock.Verify(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), "jane@example.com", "Patient", "Jane Smith", It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_ReturnsInvalidCredentials()
    {
        var (handler, _, _, _) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        var (handler, _, hasherMock, _) = CreateHandler([ActiveUser()]);
        hasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error);
    }

    [Fact]
    public async Task HandleAsync_InactiveUser_ReturnsAccountInactive()
    {
        var user = ActiveUser();
        user.Status = UserStatus.Suspended;
        var (handler, _, _, _) = CreateHandler([user]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_INACTIVE", result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyEmail_ReturnsInvalidCredentials()
    {
        var (handler, _, _, _) = CreateHandler([ActiveUser()]);

        var result = await handler.HandleAsync(new LoginCommand("", "password", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error);
    }

    [Fact]
    public async Task HandleAsync_StoresRefreshToken()
    {
        var (handler, dbMock, _, _) = CreateHandler([ActiveUser()]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        dbMock.Verify(d => d.RefreshTokens, Times.Once);
        dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmailNormalized()
    {
        var (handler, _, _, jwtMock) = CreateHandler([ActiveUser()]);

        var result = await handler.HandleAsync(new LoginCommand("  JANE@Example.COM  ", "Str0ng!Pass", null));

        Assert.True(result.IsSuccess);
        jwtMock.Verify(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), "jane@example.com", "Patient", "Jane Smith", It.IsAny<Guid?>()), Times.Once);
    }
}
