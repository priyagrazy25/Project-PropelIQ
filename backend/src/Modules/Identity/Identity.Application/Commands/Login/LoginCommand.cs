namespace Identity.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? DeviceId);
