using Notification.Application.Abstractions;

namespace Notification.Application.Services;

public interface IPdfConfirmationService
{
    /// <summary>
    /// Generates a PDF confirmation, sends it via email, and stores it for dashboard download.
    /// Falls back to text-only email if PDF generation fails.
    /// </summary>
    Task GenerateAndSendConfirmationAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the stored PDF for dashboard preview/download.
    /// </summary>
    Task<(byte[] Content, string FileName)?> GetPdfAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the stored PDF or generates it on-demand (without sending email).
    /// </summary>
    Task<(byte[] Content, string FileName)?> GetOrGeneratePdfAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
