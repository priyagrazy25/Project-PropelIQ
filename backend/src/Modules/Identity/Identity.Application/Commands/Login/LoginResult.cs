namespace Identity.Application.Commands.Login;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Role,
    string FullName);
