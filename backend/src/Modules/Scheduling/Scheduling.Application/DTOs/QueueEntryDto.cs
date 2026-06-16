namespace Scheduling.Application.DTOs;

public sealed record QueueEntryDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string AppointmentType,
    string ProviderName,
    string Status,
    DateTime? ArrivalTime,
    int WaitDurationMinutes,
    int Position,
    string RowVersion);
