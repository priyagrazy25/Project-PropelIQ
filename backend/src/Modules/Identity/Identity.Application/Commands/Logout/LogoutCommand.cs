namespace Identity.Application.Commands.Logout;

public sealed record LogoutCommand(Guid UserId, string? Jti, TimeSpan? AccessTokenRemainingLifetime);
