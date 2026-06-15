using CoreAlign.Application.Common.Outbox;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class OutboxDrainJob
{
    private readonly IOutboxProcessor _processor;
    private readonly ILogger<OutboxDrainJob> _logger;

    public OutboxDrainJob(IOutboxProcessor processor, ILogger<OutboxDrainJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Outbox drain job starting.");
        await _processor.DrainAsync(cancellationToken);
        _logger.LogDebug("Outbox drain job completed.");
    }
}
