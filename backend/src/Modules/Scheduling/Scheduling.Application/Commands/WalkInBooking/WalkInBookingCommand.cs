namespace Scheduling.Application.Commands.WalkInBooking;

public sealed record WalkInBookingCommand(
    Guid? ExistingPatientId,
    string? PatientName,
    string? PatientEmail,
    string? PatientPhone,
    DateOnly? PatientDateOfBirth,
    Guid ProviderId,
    string? Reason,
    Guid StaffUserId);
