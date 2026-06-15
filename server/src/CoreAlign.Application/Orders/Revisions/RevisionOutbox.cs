using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Orders.Revisions;

public record OrderRevisionRequestedPayload(
    Guid TenantId,
    Guid OrderId,
    Guid RevisionId,
    int RevisionNumber,
    string OrderNumber,
    Guid RequestedByUserId,
    string RequestedByPersona,
    Guid CustomerId,
    Guid? OriginDealerAccountId,
    Guid? OriginDealerUserId,
    Guid? OriginCustomerUserId);

public record OrderRevisionApprovedPayload(
    Guid TenantId,
    Guid OrderId,
    Guid RevisionId,
    int RevisionNumber,
    string OrderNumber,
    Guid ApprovedByUserId,
    Guid RequestedByUserId,
    string RequestedByPersona,
    Guid CustomerId,
    decimal NewTotal,
    string Currency);

public record OrderRevisionRejectedPayload(
    Guid TenantId,
    Guid OrderId,
    Guid RevisionId,
    int RevisionNumber,
    string OrderNumber,
    Guid RejectedByUserId,
    string Reason,
    Guid RequestedByUserId);

public interface IOrderRevisionOutbox
{
    Task EnqueueRequestedAsync(OrderRevisionRequestedPayload payload, CancellationToken cancellationToken = default);
    Task EnqueueApprovedAsync(OrderRevisionApprovedPayload payload, CancellationToken cancellationToken = default);
    Task EnqueueRejectedAsync(OrderRevisionRejectedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class OrderRevisionOutbox : IOrderRevisionOutbox
{
    public const string RequestedMessageType = "OrderRevisionRequested";
    public const string ApprovedMessageType = "OrderRevisionApproved";
    public const string RejectedMessageType = "OrderRevisionRejected";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public OrderRevisionOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public Task EnqueueRequestedAsync(OrderRevisionRequestedPayload payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(RequestedMessageType, payload, cancellationToken);

    public Task EnqueueApprovedAsync(OrderRevisionApprovedPayload payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(ApprovedMessageType, payload, cancellationToken);

    public Task EnqueueRejectedAsync(OrderRevisionRejectedPayload payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(RejectedMessageType, payload, cancellationToken);

    private async Task EnqueueAsync<T>(string messageType, T payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(messageType, json), cancellationToken);
        _signal.MarkPending();
    }
}
