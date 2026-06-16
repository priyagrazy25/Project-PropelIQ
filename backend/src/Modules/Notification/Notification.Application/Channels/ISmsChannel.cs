namespace Notification.Application.Channels;

public interface ISmsChannel
{
    Task<ChannelResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
