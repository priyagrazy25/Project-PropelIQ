namespace Identity.Application.Commands.ExtendSession;

public sealed record ExtendSessionResult(string AccessToken, string RefreshToken);
