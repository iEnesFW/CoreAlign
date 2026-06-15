using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Jobs;

public class OutboxDrainJobTests
{
    private readonly IOutboxProcessor _processor = Substitute.For<IOutboxProcessor>();

    [Fact]
    public async Task Delegates_to_processor_drain()
    {
        var sut = new OutboxDrainJob(_processor, NullLogger<OutboxDrainJob>.Instance);

        await sut.RunAsync(CancellationToken.None);

        await _processor.Received(1).DrainAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_cancellation_token_to_processor()
    {
        var sut = new OutboxDrainJob(_processor, NullLogger<OutboxDrainJob>.Instance);
        using var cts = new CancellationTokenSource();

        await sut.RunAsync(cts.Token);

        await _processor.Received(1).DrainAsync(cts.Token);
    }

    [Fact]
    public async Task No_op_when_processor_drains_empty_batch()
    {
        var sut = new OutboxDrainJob(_processor, NullLogger<OutboxDrainJob>.Instance);
        _processor.DrainAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await sut.RunAsync(CancellationToken.None);

        await _processor.Received(1).DrainAsync(Arg.Any<CancellationToken>());
    }
}
