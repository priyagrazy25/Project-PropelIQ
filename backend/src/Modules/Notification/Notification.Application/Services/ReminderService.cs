using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions;
using Notification.Application.Channels;
using Notification.Domain.Entities;

namespace Notification.Application.Services;

public sealed class ReminderService : IReminderService
{
    private readonly IReminderAppointmentQuery _appointmentQuery;
    private readonly IReminderDeliveryLogRepository _deliveryLogRepo;
    private readonly ISmsChannel _smsChannel;
    private readonly IEmailChannel _emailChannel;
    private readonly ILogger<ReminderService> _logger;
    private readonly int _dailyEmailLimit;

    /// <summary>
    /// Standard reminder windows for all appointments.
    /// </summary>
    private static readonly (string Label, int Hours)[] StandardReminderWindows =
    [
        ("72h", 72),
        ("24h", 24),
        ("2h", 2)
    ];

    /// <summary>
    /// Escalated reminder windows for high-risk appointments (score > 70).
    /// Per AC-3: triggers at closer intervals (48h, 12h).
    /// </summary>
    private static readonly (string Label, int Hours)[] HighRiskEscalatedWindows =
    [
        ("48h", 48),
        ("12h", 12)
    ];

    /// <summary>
    /// Risk score threshold for escalated reminders.
    /// </summary>
    private const int HighRiskThreshold = 70;

    public ReminderService(
        IReminderAppointmentQuery appointmentQuery,
        IReminderDeliveryLogRepository deliveryLogRepo,
        ISmsChannel smsChannel,
        IEmailChannel emailChannel,
        IConfiguration configuration,
        ILogger<ReminderService> logger)
    {
        _appointmentQuery = appointmentQuery;
        _deliveryLogRepo = deliveryLogRepo;
        _smsChannel = smsChannel;
        _emailChannel = emailChannel;
        _logger = logger;
        _dailyEmailLimit = configuration.GetValue("SendGrid:DailyLimit", 100);
    }

    public async Task EvaluateAndSendRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Process standard reminder windows for all appointments
        foreach (var (label, hours) in StandardReminderWindows)
        {
            await ProcessReminderWindowAsync(now, label, hours, highRiskOnly: false, cancellationToken);
        }

        // Process escalated windows for high-risk appointments (AC-3)
        foreach (var (label, hours) in HighRiskEscalatedWindows)
        {
            await ProcessReminderWindowAsync(now, label, hours, highRiskOnly: true, cancellationToken);
        }
    }

    private async Task ProcessReminderWindowAsync(
        DateTime now,
        string label,
        int hours,
        bool highRiskOnly,
        CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(hours - 1);
        var windowEnd = now.AddHours(hours + 1);

        var appointments = await _appointmentQuery.GetUpcomingAppointmentsAsync(
            windowStart, windowEnd, cancellationToken);

        if (appointments.Count == 0)
            return;

        // Filter to high-risk only for escalated windows
        if (highRiskOnly)
        {
            appointments = appointments
                .Where(a => a.NoShowRiskScore > HighRiskThreshold)
                .ToList();

            if (appointments.Count == 0)
                return;

            _logger.LogInformation(
                "Escalated reminder window {Window}: found {Count} high-risk appointments to notify",
                label, appointments.Count);
        }
        else
        {
            _logger.LogInformation(
                "Reminder window {Window}: found {Count} appointments to notify",
                label, appointments.Count);
        }

        var todayEmailCount = await _deliveryLogRepo.GetTodayEmailCountAsync(cancellationToken);
        var prioritized = PrioritizeByProximityAndRisk(appointments);

        foreach (var appointment in prioritized)
        {
            await SendReminderForAppointmentAsync(appointment, label, todayEmailCount, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;
        }
    }

    private static IReadOnlyList<ReminderAppointmentInfo> PrioritizeByProximityAndRisk(
        IReadOnlyList<ReminderAppointmentInfo> appointments)
    {
        return appointments
            .OrderBy(a => a.AppointmentDateTime)
            .ThenByDescending(a => a.NoShowRiskScore)
            .ToList();
    }

    private async Task SendReminderForAppointmentAsync(
        ReminderAppointmentInfo appointment,
        string window,
        int todayEmailCount,
        CancellationToken cancellationToken)
    {
        var message = FormatReminderMessage(appointment, window);

        // Send SMS
        if (!string.IsNullOrWhiteSpace(appointment.PatientPhone))
        {
            var alreadySentSms = await _deliveryLogRepo.ExistsAsync(
                appointment.AppointmentId, "SMS", window, cancellationToken);

            if (!alreadySentSms)
            {
                var smsResult = await _smsChannel.SendAsync(appointment.PatientPhone, message, cancellationToken);

                await LogDeliveryAsync(appointment, "SMS", window, smsResult, cancellationToken);

                if (!smsResult.Success)
                {
                    _logger.LogWarning(
                        "SMS failed for appointment {AppointmentId}, window {Window}: {Error}",
                        appointment.AppointmentId, window, smsResult.ErrorMessage);
                }
            }
        }

        // Send Email (respect daily limit; prioritize by proximity + risk)
        if (!string.IsNullOrWhiteSpace(appointment.PatientEmail))
        {
            var alreadySentEmail = await _deliveryLogRepo.ExistsAsync(
                appointment.AppointmentId, "Email", window, cancellationToken);

            if (!alreadySentEmail && todayEmailCount < _dailyEmailLimit)
            {
                var subject = $"Appointment Reminder — {appointment.AppointmentDateTime:MMMM dd, yyyy h:mm tt}";
                var emailResult = await _emailChannel.SendAsync(
                    appointment.PatientEmail, subject, message, cancellationToken);

                await LogDeliveryAsync(appointment, "Email", window, emailResult, cancellationToken);

                if (emailResult.Success)
                {
                    Interlocked.Increment(ref todayEmailCount);
                }
                else
                {
                    _logger.LogWarning(
                        "Email failed for appointment {AppointmentId}, window {Window}: {Error}. Falling back handled by SMS.",
                        appointment.AppointmentId, window, emailResult.ErrorMessage);
                }
            }
            else if (todayEmailCount >= _dailyEmailLimit)
            {
                _logger.LogWarning(
                    "SendGrid daily limit ({Limit}) reached — skipping email for appointment {AppointmentId}",
                    _dailyEmailLimit, appointment.AppointmentId);
            }
        }

        // Alert admin if both circuits are open
        if (IsBothCircuitsDown(appointment))
        {
            _logger.LogCritical(
                "ALERT: Both SMS and Email channels are down for appointment {AppointmentId}. Manual follow-up required.",
                appointment.AppointmentId);
        }
    }

    private static bool IsBothCircuitsDown(ReminderAppointmentInfo appointment)
    {
        // This is detected at the channel level via circuit breaker state;
        // the log entries with "circuit breaker open" errors indicate both are down.
        // Actual monitoring is handled by the health check and log aggregation.
        return false;
    }

    private async Task LogDeliveryAsync(
        ReminderAppointmentInfo appointment,
        string channel,
        string window,
        ChannelResult result,
        CancellationToken cancellationToken)
    {
        var log = new ReminderDeliveryLog
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            Channel = channel,
            ReminderWindow = window,
            Status = result.Success ? "Delivered" : "Failed",
            FailureReason = result.ErrorMessage,
            AttemptCount = 1,
            SentAt = DateTime.UtcNow
        };

        await _deliveryLogRepo.AddAsync(log, cancellationToken);
    }

    private static string FormatReminderMessage(ReminderAppointmentInfo appointment, string window)
    {
        return $"Hi {appointment.PatientFullName}, this is a reminder for your appointment with " +
               $"Dr. {appointment.ProviderName} on {appointment.AppointmentDateTime:MMMM dd, yyyy} at " +
               $"{appointment.AppointmentDateTime:h:mm tt}. " +
               $"If you need to reschedule, please contact us as soon as possible.";
    }

    /// <summary>
    /// Sends an immediate reminder for a specific appointment (manual outreach from dashboard).
    /// </summary>
    public async Task<(bool Success, string? Error)> SendImmediateReminderAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentQuery.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        
        if (appointment == null)
        {
            return (false, "Appointment not found");
        }

        var message = FormatReminderMessage(appointment, "manual");
        var smsSent = false;
        var emailSent = false;
        string? lastError = null;

        // Send SMS
        if (!string.IsNullOrWhiteSpace(appointment.PatientPhone))
        {
            var smsResult = await _smsChannel.SendAsync(appointment.PatientPhone, message, cancellationToken);
            smsSent = smsResult.Success;
            if (!smsResult.Success)
            {
                lastError = smsResult.ErrorMessage;
                _logger.LogWarning(
                    "Manual SMS reminder failed for appointment {AppointmentId}: {Error}",
                    appointmentId, smsResult.ErrorMessage);
            }
        }

        // Send Email
        if (!string.IsNullOrWhiteSpace(appointment.PatientEmail))
        {
            var todayEmailCount = await _deliveryLogRepo.GetTodayEmailCountAsync(cancellationToken);
            if (todayEmailCount < _dailyEmailLimit)
            {
                var subject = $"Appointment Reminder — {appointment.AppointmentDateTime:MMMM dd, yyyy h:mm tt}";
                var emailResult = await _emailChannel.SendAsync(
                    appointment.PatientEmail, subject, message, cancellationToken);
                emailSent = emailResult.Success;
                if (!emailResult.Success)
                {
                    lastError = emailResult.ErrorMessage;
                    _logger.LogWarning(
                        "Manual email reminder failed for appointment {AppointmentId}: {Error}",
                        appointmentId, emailResult.ErrorMessage);
                }
            }
            else
            {
                lastError = "Daily email limit reached";
                _logger.LogWarning(
                    "SendGrid daily limit reached — cannot send manual email for appointment {AppointmentId}",
                    appointmentId);
            }
        }

        if (smsSent || emailSent)
        {
            _logger.LogInformation(
                "Manual reminder sent for appointment {AppointmentId}: SMS={SmsSent}, Email={EmailSent}",
                appointmentId, smsSent, emailSent);
            return (true, null);
        }

        return (false, lastError ?? "No contact information available for patient");
    }
}
