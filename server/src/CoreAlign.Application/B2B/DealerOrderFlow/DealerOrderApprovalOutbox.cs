using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.B2B.DealerOrderFlow;

/// <summary>
/// Payload enqueued when a dealer submits an order on behalf of a customer.
/// Drained by <c>DealerOrderSubmittedForApprovalOutboxHandler</c> to create
/// one notification per active CustomerUser in the target customer.
/// </summary>
public record DealerOrderSubmittedForApprovalPayload(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    Guid DealerAccountId,
    string DealerName,
    int LineCount,
    decimal Total,
    string Currency,
    Guid? DealerUserId);

/// <summary>
/// Payload enqueued after a customer approves a dealer-submitted order. Drains
/// into a notification for the dealer user that submitted the order and one
/// for every active TenantAdmin in the tenant.
/// </summary>
public record DealerOrderApprovedByCustomerPayload(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    string CustomerName,
    Guid DealerAccountId,
    string DealerName,
    Guid? DealerUserId,
    Guid ApprovedByUserId,
    int LineCount,
    decimal Total,
    string Currency);

/// <summary>
/// Payload enqueued after a customer rejects a dealer-submitted order. Drains
/// into a notification for the dealer user that submitted the order, including
/// the rejection reason verbatim so the dealer knows what to adjust.
/// </summary>
public record DealerOrderRejectedByCustomerPayload(
    Guid OrderId,
    Guid TenantId,
    Guid CustomerId,
    string CustomerName,
    Guid DealerAccountId,
    string DealerName,
    Guid? DealerUserId,
    Guid RejectedByUserId,
    string Reason);

public interface IDealerOrderApprovalOutbox
{
    Task EnqueueSubmittedForApprovalAsync(DealerOrderSubmittedForApprovalPayload payload, CancellationToken cancellationToken = default);
    Task EnqueueApprovedAsync(DealerOrderApprovedByCustomerPayload payload, CancellationToken cancellationToken = default);
    Task EnqueueRejectedAsync(DealerOrderRejectedByCustomerPayload payload, CancellationToken cancellationToken = default);
}

public sealed class DealerOrderApprovalOutbox : IDealerOrderApprovalOutbox
{
    public const string SubmittedForApprovalMessageType = "DealerOrderSubmittedForApproval";
    public const string ApprovedByCustomerMessageType = "DealerOrderApprovedByCustomer";
    public const string RejectedByCustomerMessageType = "DealerOrderRejectedByCustomer";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public DealerOrderApprovalOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public Task EnqueueSubmittedForApprovalAsync(DealerOrderSubmittedForApprovalPayload payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(SubmittedForApprovalMessageType, payload, cancellationToken);

    public Task EnqueueApprovedAsync(DealerOrderApprovedByCustomerPayload payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(ApprovedByCustomerMessageType, payload, cancellationToken);

    public Task EnqueueRejectedAsync(DealerOrderRejectedByCustomerPayload payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(RejectedByCustomerMessageType, payload, cancellationToken);

    private async Task EnqueueAsync<T>(string messageType, T payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(messageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static T? Deserialize<T>(string payloadJson) where T : class =>
        JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
}
