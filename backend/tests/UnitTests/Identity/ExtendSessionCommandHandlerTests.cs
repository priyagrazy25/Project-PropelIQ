using Identity.Application.Abstractions;
using Identity.Application.Commands.ExtendSession;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests.Identity;

public sealed class ExtendSessionCommandHandlerTests
{
    private static User ActiveUser() => new()
    {
        Email = "jane@example.com",
        PasswordHash = "hashed",
        FullName = "Jane Smith",
        Role = UserRole.Patient,
        Status = UserStatus.Active,
    };

    private static RefreshToken ValidToken(User user) => new()
    {
        UserId = user.Id,
        Token = "valid-refresh-token",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        User = user,
    };

    private static (ExtendSessionCommandHandler handler, Mock<IIdentityDbContext> dbMock,
        Mock<IJwtTokenService> jwtMock, Mock<ISessionTrackingService> sessionMock)
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

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .Returns("new-access-token");
        jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");

        var sessionMock = new Mock<ISessionTrackingService>();
        sessionMock.Setup(s => s.TrackSessionAsync(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionMock.Setup(s => s.InvalidateAllSessionsAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ExtendSessionCommandHandler(dbMock.Object, jwtMock.Object, sessionMock.Object);
        return (handler, dbMock, jwtMock, sessionMock);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_ReturnsNewTokensAndTracksSession()
    {
        var user = ActiveUser();
        var token = ValidToken(user);
        var (handler, _, _, sessionMock) = CreateHandler([token]);

        var result = await handler.HandleAsync(new ExtendSessionCommand("valid-refresh-token", "device-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Value!.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
        Assert.True(token.IsRevoked);
        sessionMock.Verify(s => s.TrackSessionAsync(
            user.Id, "device-1", TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyToken_ReturnsInvalidToken()
    {
        var (handler, _, _, _) = CreateHandler();

        var result = await handler.HandleAsync(new ExtendSessionCommand("", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TOKEN", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ReturnsSessionExpired()
    {
        var user = ActiveUser();
        var expired = ValidToken(user);
        expired.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        var (handler, _, _, _) = CreateHandler([expired]);

        var result = await handler.HandleAsync(new ExtendSessionCommand("valid-refresh-token", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_EXPIRED", result.Error);
    }

    [Fact]
    public async Task HandleAsync_RevokedToken_DetectsReuseAndInvalidatesSessions()
    {
        var user = ActiveUser();
        var revoked = ValidToken(user);
        revoked.IsRevoked = true;
        var (handler, _, _, sessionMock) = CreateHandler([revoked]);

        var result = await handler.HandleAsync(new ExtendSessionCommand("valid-refresh-token", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("TOKEN_REUSE_DETECTED", result.Error);
        sessionMock.Verify(s => s.InvalidateAllSessionsAsync(
            user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InactiveUser_ReturnsAccountInactive()
    {
        var user = ActiveUser();
        user.Status = UserStatus.Suspended;
        var token = ValidToken(user);
        var (handler, _, _, _) = CreateHandler([token]);

        var result = await handler.HandleAsync(new ExtendSessionCommand("valid-refresh-token", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_INACTIVE", result.Error);
    }
}
