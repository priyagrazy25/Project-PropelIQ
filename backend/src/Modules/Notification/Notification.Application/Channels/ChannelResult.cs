namespace Notification.Application.Channels;

public sealed record ChannelResult(bool Success, string? ErrorMessage = null);
