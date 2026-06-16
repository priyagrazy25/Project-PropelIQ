namespace Notification.Application.Channels;

public interface IEmailChannel
{
    Task<ChannelResult> SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);

    Task<ChannelResult> SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        byte[] attachmentContent,
        string attachmentFileName,
        string attachmentMimeType,
        CancellationToken cancellationToken = default);
}
