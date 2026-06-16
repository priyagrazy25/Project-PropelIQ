namespace Scheduling.Application.Commands.WalkInBooking;

public sealed record PatientSearchResult(
    Guid PatientId,
    Guid UserId,
    string FullName,
    string Email,
    string? ContactNumber,
    DateOnly? DateOfBirth);
