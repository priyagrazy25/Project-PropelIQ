namespace Identity.Application.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string DateOfBirth,
    string? Gender,
    string Password);
