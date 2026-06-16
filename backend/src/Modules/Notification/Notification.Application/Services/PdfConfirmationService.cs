using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions;
using Notification.Application.Channels;
using Notification.Application.Documents;
using Notification.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Notification.Application.Services;

public sealed class PdfConfirmationService : IPdfConfirmationService
{
    private readonly IConfirmationAppointmentQuery _appointmentQuery;
    private readonly IPdfConfirmationRepository _pdfRepo;
    private readonly IEmailChannel _emailChannel;
    private readonly ILogger<PdfConfirmationService> _logger;

    public PdfConfirmationService(
        IConfirmationAppointmentQuery appointmentQuery,
        IPdfConfirmationRepository pdfRepo,
        IEmailChannel emailChannel,
        ILogger<PdfConfirmationService> logger)
    {
        _appointmentQuery = appointmentQuery;
        _pdfRepo = pdfRepo;
        _emailChannel = emailChannel;
        _logger = logger;
    }

    public async Task GenerateAndSendConfirmationAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentQuery.GetAppointmentForConfirmationAsync(
            appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning(
                "Cannot generate PDF confirmation: appointment {AppointmentId} not found",
                appointmentId);
            return;
        }

        byte[]? pdfBytes = null;
        string? fileName = null;
        bool pdfGenerated = false;

        try
        {
            // Generate PDF using QuestPDF
            var document = new AppointmentPdfDocument(appointment);
            pdfBytes = document.GeneratePdf();
            fileName = $"Confirmation_{appointment.AppointmentId:N}.pdf";
            pdfGenerated = true;

            _logger.LogInformation(
                "PDF confirmation generated for appointment {AppointmentId}, size: {Size} bytes",
                appointmentId, pdfBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate PDF for appointment {AppointmentId}. Falling back to text-only email",
                appointmentId);
        }

        // Store PDF for dashboard download (even if email fails later)
        PdfConfirmationRecord? record = null;
        if (pdfGenerated && pdfBytes is not null)
        {
            record = await StorePdfRecordAsync(
                appointment, pdfBytes, fileName!, cancellationToken);
        }

        // Send email with PDF attachment or text-only fallback
        if (string.IsNullOrWhiteSpace(appointment.PatientEmail))
        {
            _logger.LogWarning(
                "No email address for patient {PatientId}, skipping email for appointment {AppointmentId}",
                appointment.PatientId, appointmentId);
            return;
        }

        var subject = $"Appointment Confirmation — {appointment.AppointmentDateTime:MMMM dd, yyyy h:mm tt}";

        if (pdfGenerated && pdfBytes is not null)
        {
            await SendEmailWithPdfAsync(
                appointment, subject, pdfBytes, fileName!, record, cancellationToken);
        }
        else
        {
            await SendTextOnlyFallbackAsync(
                appointment, subject, record, cancellationToken);
        }
    }

    public async Task<(byte[] Content, string FileName)?> GetPdfAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _pdfRepo.GetByAppointmentIdAsync(appointmentId, cancellationToken);

        if (record is null || record.PdfContent.Length == 0)
        {
            return null;
        }

        return (record.PdfContent, record.FileName);
    }

    public async Task<(byte[] Content, string FileName)?> GetOrGeneratePdfAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        // Try to return stored PDF first
        var existing = await GetPdfAsync(appointmentId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        // No PDF stored — generate on-demand without emailing
        var appointment = await _appointmentQuery.GetAppointmentForConfirmationAsync(
            appointmentId, cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        try
        {
            var document = new AppointmentPdfDocument(appointment);
            var pdfBytes = document.GeneratePdf();
            var fileName = $"Confirmation_{appointment.AppointmentId:N}.pdf";

            await StorePdfRecordAsync(appointment, pdfBytes, fileName, cancellationToken);

            _logger.LogInformation(
                "On-demand PDF generated for appointment {AppointmentId}",
                appointmentId);

            return (pdfBytes, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "On-demand PDF generation failed for appointment {AppointmentId}",
                appointmentId);
            return null;
        }
    }

    private async Task<PdfConfirmationRecord> StorePdfRecordAsync(
        AppointmentConfirmationInfo appointment,
        byte[] pdfBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        // Check if a record already exists (e.g. reschedule scenario)
        var existing = await _pdfRepo.GetByAppointmentIdAsync(
            appointment.AppointmentId, cancellationToken);

        if (existing is not null)
        {
            // Reschedule: update the existing record with new PDF
            existing.PdfContent = pdfBytes;
            existing.FileName = fileName;
            existing.Status = "Regenerated";
            existing.DeliveryStatus = null;
            existing.FailureReason = null;
            existing.GeneratedAt = DateTime.UtcNow;

            await _pdfRepo.UpdateAsync(existing, cancellationToken);

            _logger.LogInformation(
                "PDF record updated (reschedule) for appointment {AppointmentId}",
                appointment.AppointmentId);

            return existing;
        }

        var record = new PdfConfirmationRecord
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            PdfContent = pdfBytes,
            FileName = fileName,
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow
        };

        await _pdfRepo.AddAsync(record, cancellationToken);

        _logger.LogInformation(
            "PDF record stored for appointment {AppointmentId}",
            appointment.AppointmentId);

        return record;
    }

    private async Task SendEmailWithPdfAsync(
        AppointmentConfirmationInfo appointment,
        string subject,
        byte[] pdfBytes,
        string fileName,
        PdfConfirmationRecord? record,
        CancellationToken cancellationToken)
    {
        var body = FormatConfirmationEmailBody(appointment);

        var result = await _emailChannel.SendWithAttachmentAsync(
            appointment.PatientEmail!,
            subject,
            body,
            pdfBytes,
            fileName,
            "application/pdf",
            cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "Confirmation email with PDF sent for appointment {AppointmentId}",
                appointment.AppointmentId);

            if (record is not null)
            {
                record.DeliveryStatus = "Sent";
                await _pdfRepo.UpdateAsync(record, cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning(
                "Email with PDF failed for appointment {AppointmentId}: {Error}. Retrying with text-only fallback",
                appointment.AppointmentId, result.ErrorMessage);

            if (record is not null)
            {
                record.DeliveryStatus = "PdfEmailFailed";
                record.FailureReason = result.ErrorMessage;
                await _pdfRepo.UpdateAsync(record, cancellationToken);
            }

            // Fallback to text-only email
            await SendTextOnlyFallbackAsync(
                appointment, subject, record, cancellationToken);
        }
    }

    private async Task SendTextOnlyFallbackAsync(
        AppointmentConfirmationInfo appointment,
        string subject,
        PdfConfirmationRecord? record,
        CancellationToken cancellationToken)
    {
        var body = FormatConfirmationEmailBody(appointment);

        var result = await _emailChannel.SendAsync(
            appointment.PatientEmail!,
            subject,
            body,
            cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "Text-only confirmation email sent for appointment {AppointmentId}",
                appointment.AppointmentId);

            if (record is not null)
            {
                record.DeliveryStatus = "TextOnlySent";
                await _pdfRepo.UpdateAsync(record, cancellationToken);
            }
        }
        else
        {
            _logger.LogError(
                "Text-only fallback email also failed for appointment {AppointmentId}: {Error}",
                appointment.AppointmentId, result.ErrorMessage);

            if (record is not null)
            {
                record.DeliveryStatus = "AllDeliveryFailed";
                record.FailureReason = result.ErrorMessage;
                await _pdfRepo.UpdateAsync(record, cancellationToken);
            }
        }
    }

    private static string FormatConfirmationEmailBody(AppointmentConfirmationInfo appointment)
    {
        var prepSection = string.IsNullOrWhiteSpace(appointment.PrepNotes)
            ? string.Empty
            : $"\n\nPreparation Notes:\n{appointment.PrepNotes}";

        return $"""
            Dear {appointment.PatientFullName},

            Your appointment has been confirmed with the following details:

            Provider: Dr. {appointment.ProviderName}
            Date: {appointment.AppointmentDateTime:dddd, MMMM dd, yyyy}
            Time: {appointment.AppointmentDateTime:h:mm tt}
            Duration: {appointment.DurationMinutes} minutes
            Type: {appointment.AppointmentType}
            Location: {appointment.Location ?? "To be determined"}
            {prepSection}

            Please arrive 15 minutes before your scheduled appointment time.
            Bring a valid photo ID and your insurance card.

            If you need to cancel or reschedule, please contact us at least 24 hours in advance.

            Thank you,
            Unified Patient Access Platform
            """;
    }
}
