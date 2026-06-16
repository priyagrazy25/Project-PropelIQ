using System.Threading.Channels;

namespace Scheduling.Application.Services;

/// <summary>
/// Channel for publishing slot-released events that the waitlist notification worker processes.
/// Separate from SlotReleasedChannel (used by swap engine) to allow independent consumers.
/// </summary>
public sealed class WaitlistSlotReleasedChannel
{
    private readonly Channel<SlotReleasedEvent> _channel = Channel.CreateUnbounded<SlotReleasedEvent>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<SlotReleasedEvent> Writer => _channel.Writer;
    public ChannelReader<SlotReleasedEvent> Reader => _channel.Reader;
}
