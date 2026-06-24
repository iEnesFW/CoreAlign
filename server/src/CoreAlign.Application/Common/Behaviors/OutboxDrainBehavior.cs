using CoreAlign.Application.Common.Outbox;
using MediatR;

namespace CoreAlign.Application.Common.Behaviors;

/// <summary>
/// Drains the transactional outbox after the request's transaction has
/// committed. Registered outside <see cref="TransactionBehavior{TRequest,TResponse}"/>
/// so <c>next()</c> includes the commit; only drains when something was actually
/// enqueued (via <see cref="IOutboxSignal"/>) and only on success.
/// </summary>
public class OutboxDrainBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOutboxSignal _signal;
    private readonly IOutboxProcessor _processor;

    public OutboxDrainBehavior(IOutboxSignal signal, IOutboxProcessor processor)
    {
        _signal = signal;
        _processor = processor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        if (_signal.HasPending)
        {
            await _processor.DrainCurrentTenantAsync(cancellationToken);
        }
        return response;
    }
}
