namespace Identity.Application.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string? DeviceId);
