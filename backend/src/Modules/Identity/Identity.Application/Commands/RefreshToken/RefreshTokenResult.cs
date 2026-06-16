namespace Identity.Application.Commands.RefreshToken;

public sealed record RefreshTokenResult(string AccessToken, string RefreshToken);
