using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Common.Outbox;

public record OutboxMessageDto(
    Guid Id,
    string Type,
    OutboxStatus Status,
    int Attempts,
    int MaxAttempts,
    string? Result,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    DateTime? NextAttemptUtc);

public record ListOutboxMessagesQuery(OutboxStatus? Status = null, int Max = 100)
    : IRequest<IReadOnlyList<OutboxMessageDto>>;

/// <summary>Requeues every Deferred/Failed message for the tenant and drains immediately.</summary>
public record ReplayOutboxCommand() : IRequest<int>;

public class ListOutboxMessagesHandler : IRequestHandler<ListOutboxMessagesQuery, IReadOnlyList<OutboxMessageDto>>
{
    private readonly IOutboxRepository _outbox;
    public ListOutboxMessagesHandler(IOutboxRepository outbox) => _outbox = outbox;

    public async Task<IReadOnlyList<OutboxMessageDto>> Handle(ListOutboxMessagesQuery q, CancellationToken ct)
    {
        var max = Math.Clamp(q.Max, 1, 500);
        var rows = await _outbox.ListAsync(q.Status, max, ct);
        return rows.Select(m => new OutboxMessageDto(
            m.Id, m.Type, m.Status, m.Attempts, m.MaxAttempts, m.Result, m.LastError, m.CreatedAtUtc, m.ProcessedAtUtc, m.NextAttemptUtc)).ToList();
    }
}

public class ReplayOutboxHandler : IRequestHandler<ReplayOutboxCommand, int>
{
    private readonly IOutboxRepository _outbox;
    private readonly IOutboxProcessor _processor;
    private readonly IUnitOfWork _uow;

    public ReplayOutboxHandler(IOutboxRepository outbox, IOutboxProcessor processor, IUnitOfWork uow)
    {
        _outbox = outbox;
        _processor = processor;
        _uow = uow;
    }

    public async Task<int> Handle(ReplayOutboxCommand c, CancellationToken ct)
    {
        var deferred = await _outbox.ListAsync(OutboxStatus.Deferred, 500, ct);
        var failed = await _outbox.ListAsync(OutboxStatus.Failed, 500, ct);
        var deadLettered = await _outbox.ListAsync(OutboxStatus.DeadLetter, 500, ct);
        var stuck = deferred.Concat(failed).Concat(deadLettered).ToList();
        if (stuck.Count == 0) return 0;

        foreach (var message in stuck)
        {
            message.Requeue();
            _outbox.Update(message);
        }
        await _uow.SaveChangesAsync(ct);

        await _processor.DrainCurrentTenantAsync(ct);
        return stuck.Count;
    }
}
