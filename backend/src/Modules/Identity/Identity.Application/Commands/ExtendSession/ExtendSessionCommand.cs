namespace Identity.Application.Commands.ExtendSession;

public sealed record ExtendSessionCommand(string RefreshToken, string? DeviceId);
