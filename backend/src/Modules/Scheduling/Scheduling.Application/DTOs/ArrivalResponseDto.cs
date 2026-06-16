namespace Scheduling.Application.DTOs;

public sealed record ArrivalResponseDto(
    Guid AppointmentId,
    Guid PatientId,
    string PatientName,
    string Status,
    DateTime ArrivedAt);
