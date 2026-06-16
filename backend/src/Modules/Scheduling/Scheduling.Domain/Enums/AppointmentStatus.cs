namespace Scheduling.Domain.Enums;

public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    Arrived = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6,
    Rescheduled = 7
}
