namespace Notification.Application.Services;

public interface IReminderService
{
    Task EvaluateAndSendRemindersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends an immediate reminder for a specific appointment (manual outreach).
    /// </summary>
    Task<(bool Success, string? Error)> SendImmediateReminderAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
