using System.Threading.Channels;
using CoreAlign.Domain.Entities;

namespace CoreAlign.API.HostedServices;

public interface IActivityLogChannel
{
    bool TryEnqueue(ActivityLog log);
    IAsyncEnumerable<ActivityLog> ReadAllAsync(CancellationToken cancellationToken);
}

public class ActivityLogChannel : IActivityLogChannel
{
    // Bounded channel so a slow DB can't grow the queue without limit. DropOldest
    // keeps the most recent events when the DB is far behind — losing the
    // oldest tail is preferable to backpressuring request threads.
    private const int Capacity = 4096;

    private readonly Channel<ActivityLog> _channel = Channel.CreateBounded<ActivityLog>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    public bool TryEnqueue(ActivityLog log) => _channel.Writer.TryWrite(log);

    public IAsyncEnumerable<ActivityLog> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
