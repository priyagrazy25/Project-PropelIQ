using Microsoft.Extensions.Logging;
using Notification.Application.Services;
using Scheduling.Application.Abstractions;

namespace Host.Services;

/// <summary>
/// Bridges the Scheduling module's booking confirmation trigger
/// to the Notification module's PDF + email service.
/// </summary>
public sealed class BookingConfirmationNotifier : IBookingConfirmationNotifier
{
    private readonly IPdfConfirmationService _pdfConfirmationService;
    private readonly ILogger<BookingConfirmationNotifier> _logger;

    public BookingConfirmationNotifier(
        IPdfConfirmationService pdfConfirmationService,
        ILogger<BookingConfirmationNotifier> logger)
    {
        _pdfConfirmationService = pdfConfirmationService;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending booking confirmation email with PDF for appointment {AppointmentId}",
            appointmentId);

        await _pdfConfirmationService.GenerateAndSendConfirmationAsync(appointmentId, cancellationToken);
    }
}
