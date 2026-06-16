using Identity.Application.Abstractions;
using Identity.Application.Commands.Logout;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests.Identity;

public sealed class LogoutCommandHandlerTests
{
    private static User ActiveUser() => new()
    {
        Email = "jane@example.com",
        PasswordHash = "hashed",
        FullName = "Jane Smith",
        Role = UserRole.Patient,
        Status = UserStatus.Active,
    };

    private static (LogoutCommandHandler handler, Mock<IIdentityDbContext> dbMock,
        Mock<ISessionTrackingService> sessionMock, Mock<ITokenBlacklistService> blacklistMock)
        CreateHandler(List<RefreshToken>? tokens = null)
    {
        tokens ??= [];

        var tokensQueryable = tokens.AsQueryable();
        var mockTokenSet = new Mock<DbSet<RefreshToken>>();
        mockTokenSet.As<IQueryable<RefreshToken>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<RefreshToken>(tokensQueryable.Provider));
        mockTokenSet.As<IQueryable<RefreshToken>>().Setup(m => m.Expression).Returns(tokensQueryable.Expression);
        mockTokenSet.As<IQueryable<RefreshToken>>().Setup(m => m.ElementType).Returns(tokensQueryable.ElementType);
        mockTokenSet.As<IQueryable<RefreshToken>>().Setup(m => m.GetEnumerator()).Returns(tokensQueryable.GetEnumerator());
        mockTokenSet.As<IAsyncEnumerable<RefreshToken>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<RefreshToken>(tokensQueryable.GetEnumerator()));

        var dbMock = new Mock<IIdentityDbContext>();
        dbMock.Setup(d => d.RefreshTokens).Returns(mockTokenSet.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sessionMock = new Mock<ISessionTrackingService>();
        sessionMock.Setup(s => s.InvalidateAllSessionsAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var blacklistMock = new Mock<ITokenBlacklistService>();
        blacklistMock.Setup(b => b.BlacklistTokenAsync(
            It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new LogoutCommandHandler(dbMock.Object, sessionMock.Object, blacklistMock.Object);
        return (handler, dbMock, sessionMock, blacklistMock);
    }

    [Fact]
    public async Task HandleAsync_RevokesAllRefreshTokens()
    {
        var user = ActiveUser();
        var token1 = new RefreshToken { UserId = user.Id, Token = "t1", ExpiresAt = DateTime.UtcNow.AddDays(7), User = user };
        var token2 = new RefreshToken { UserId = user.Id, Token = "t2", ExpiresAt = DateTime.UtcNow.AddDays(7), User = user };
        var (handler, _, _, _) = CreateHandler([token1, token2]);

        var result = await handler.HandleAsync(new LogoutCommand(user.Id, "jti-123", TimeSpan.FromMinutes(10)));

        Assert.True(result.IsSuccess);
        Assert.True(token1.IsRevoked);
        Assert.True(token2.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_BlacklistsCurrentAccessToken()
    {
        var user = ActiveUser();
        var (handler, _, _, blacklistMock) = CreateHandler();
        var lifetime = TimeSpan.FromMinutes(10);

        await handler.HandleAsync(new LogoutCommand(user.Id, "jti-abc", lifetime));

        blacklistMock.Verify(b => b.BlacklistTokenAsync("jti-abc", lifetime, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesCachedSessions()
    {
        var user = ActiveUser();
        var (handler, _, sessionMock, _) = CreateHandler();

        await handler.HandleAsync(new LogoutCommand(user.Id, null, null));

        sessionMock.Verify(s => s.InvalidateAllSessionsAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoJti_SkipsBlacklist()
    {
        var user = ActiveUser();
        var (handler, _, _, blacklistMock) = CreateHandler();

        await handler.HandleAsync(new LogoutCommand(user.Id, null, null));

        blacklistMock.Verify(b => b.BlacklistTokenAsync(
            It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
