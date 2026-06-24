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
    Task DrainCurrentTenantAsync(CancellationToken cancellationToken = default);
}

public sealed class OutboxProcessor : IOutboxProcessor
{
    private const int MaxBatch = 100;
    private const int MaxSaveConflictRetries = 4;

    private readonly IOutboxRepository _outbox;
    private readonly IReadOnlyDictionary<string, IOutboxMessageHandler> _handlers;
    private readonly IUnitOfWork _uow;
    private readonly IOutboxRetryPolicy _retryPolicy;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxRepository outbox,
        IEnumerable<IOutboxMessageHandler> handlers,
        IUnitOfWork uow,
        IOutboxRetryPolicy retryPolicy,
        ITenantContext tenantContext,
        ILogger<OutboxProcessor> logger)
    {
        _outbox = outbox;
        _handlers = handlers.ToDictionary(h => h.MessageType, StringComparer.Ordinal);
        _uow = uow;
        _retryPolicy = retryPolicy;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        var due = await _outbox.GetDueAcrossTenantsAsync(MaxBatch, DateTime.UtcNow, cancellationToken);
        foreach (var message in due)
        {
            using (_tenantContext.PushScope(message.TenantId))
            {
                await ProcessOneAsync(message, cancellationToken);
            }
        }
    }

    public async Task DrainCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var due = await _outbox.GetDueForCurrentTenantAsync(MaxBatch, DateTime.UtcNow, cancellationToken);
        foreach (var message in due)
        {
            await ProcessOneAsync(message, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(message.Type, out var handler))
        {
            await TransitionAsync(message.Id, m => m.MarkDeadLetter($"Unknown outbox type '{message.Type}'."), cancellationToken);
            return;
        }

        Exception? lastError = null;
        for (var attempt = 0; attempt < MaxSaveConflictRetries; attempt++)
        {
            try
            {
                var result = await handler.HandleAsync(message.PayloadJson, cancellationToken);
                ApplyResult(message, result, DateTime.UtcNow);
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

        _logger.LogWarning(lastError, "Outbox message {Id} of type {Type} threw after {Attempts} save attempts.", message.Id, message.Type, MaxSaveConflictRetries);
        var error = lastError?.GetBaseException().Message ?? "Unknown error.";
        await TransitionAsync(message.Id, m => ApplyFailure(m, error, DateTime.UtcNow), cancellationToken);
    }

    private void ApplyResult(OutboxMessage message, OutboxHandlerResult result, DateTime utcNow)
    {
        switch (result.Outcome)
        {
            case OutboxHandlerOutcome.Deferred when result.RetryAfterUtc.HasValue:
                message.DeferUntil(result.RetryAfterUtc.Value, result.ResultOrError);
                break;
            case OutboxHandlerOutcome.Deferred:
                message.MarkDeferred(result.ResultOrError);
                break;
            case OutboxHandlerOutcome.Failed:
                ApplyFailure(message, result.ResultOrError, utcNow);
                break;
            default:
                message.MarkProcessed(result.ResultOrError);
                break;
        }
    }

    private void ApplyFailure(OutboxMessage message, string error, DateTime utcNow)
    {
        if (message.HasExhaustedAttempts)
        {
            message.MarkDeadLetter(error);
            return;
        }

        message.ScheduleRetry(_retryPolicy.ComputeNextAttempt(message.Attempts + 1, utcNow), error);
    }

    private async Task TransitionAsync(Guid messageId, Action<OutboxMessage> transition, CancellationToken cancellationToken)
    {
        var fresh = await _outbox.GetByIdAsync(messageId, cancellationToken);
        if (fresh is null) return;
        transition(fresh);
        _outbox.Update(fresh);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
