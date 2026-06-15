using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Common.Outbox;

/// <summary>
/// Drains pending outbox messages after the producing transaction has
/// committed. Each message is dispatched to a type-specific
/// <see cref="IOutboxMessageHandler"/>; persistence, retries and status
/// transitions live here so individual handlers stay focused on their
/// side-effect (GL posting, notification fan-out, ...).
/// </summary>
public interface IOutboxProcessor
{
    Task DrainAsync(CancellationToken cancellationToken = default);
}

public sealed class OutboxProcessor : IOutboxProcessor
{
    private const int MaxBatch = 100;
    private const int MaxNumberRetries = 4;

    private readonly IOutboxRepository _outbox;
    private readonly IReadOnlyDictionary<string, IOutboxMessageHandler> _handlers;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxRepository outbox,
        IEnumerable<IOutboxMessageHandler> handlers,
        IUnitOfWork uow,
        ILogger<OutboxProcessor> logger)
    {
        _outbox = outbox;
        _handlers = handlers.ToDictionary(h => h.MessageType, StringComparer.Ordinal);
        _uow = uow;
        _logger = logger;
    }

    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _outbox.GetPendingAsync(MaxBatch, cancellationToken);
        foreach (var message in pending)
        {
            await ProcessOneAsync(message, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(message.Type, out var handler))
        {
            await FailAsync(message.Id, $"Unknown outbox type '{message.Type}'.", cancellationToken);
            return;
        }

        // Retry on any save failure: the dominant case is a unique-clash
        // (e.g. journal-number) with a concurrent drain, where the competing
        // commit has advanced the sequence so the next attempt picks a fresh
        // value. ClearChangeTracker drops the failed attempt's tracked
        // entities before retrying.
        Exception? lastError = null;
        for (var attempt = 0; attempt < MaxNumberRetries; attempt++)
        {
            try
            {
                var result = await handler.HandleAsync(message.PayloadJson, cancellationToken);
                ApplyResult(message, result);
                _outbox.Update(message);
                await _uow.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _uow.ClearChangeTracker();
            }
        }

        _logger.LogWarning(lastError, "Outbox message {Id} of type {Type} failed after {Attempts} attempts.", message.Id, message.Type, MaxNumberRetries);
        await FailAsync(message.Id, lastError?.GetBaseException().Message ?? "Unknown error.", cancellationToken);
    }

    private static void ApplyResult(OutboxMessage message, OutboxHandlerResult result)
    {
        switch (result.Outcome)
        {
            case OutboxHandlerOutcome.Deferred:
                message.MarkDeferred(result.ResultOrError);
                break;
            case OutboxHandlerOutcome.Failed:
                message.MarkFailed(result.ResultOrError);
                break;
            default:
                message.MarkProcessed(result.ResultOrError);
                break;
        }
    }

    private async Task FailAsync(Guid messageId, string error, CancellationToken cancellationToken)
    {
        // Re-load after a ChangeTracker.Clear so we update a tracked instance.
        var fresh = await _outbox.GetByIdAsync(messageId, cancellationToken);
        if (fresh is null) return;
        fresh.MarkFailed(error);
        _outbox.Update(fresh);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
