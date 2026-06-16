namespace Notification.Application.Abstractions;

public interface IConfirmationAppointmentQuery
{
    Task<AppointmentConfirmationInfo?> GetAppointmentForConfirmationAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
