namespace Scheduling.Application.Abstractions;

/// <summary>
/// Sends booking confirmation email with PDF attachment immediately after appointment booking.
/// </summary>
public interface IBookingConfirmationNotifier
{
    Task SendConfirmationAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
