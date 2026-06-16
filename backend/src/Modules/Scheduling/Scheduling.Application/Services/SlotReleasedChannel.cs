using System.Threading.Channels;

namespace Scheduling.Application.Services;

/// <summary>
/// Channel for publishing slot-released events that the swap background worker processes.
/// </summary>
public sealed class SlotReleasedChannel
{
    private readonly Channel<SlotReleasedEvent> _channel = Channel.CreateUnbounded<SlotReleasedEvent>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<SlotReleasedEvent> Writer => _channel.Writer;
    public ChannelReader<SlotReleasedEvent> Reader => _channel.Reader;
}

public sealed record SlotReleasedEvent(Guid SlotId, Guid ProviderId);
