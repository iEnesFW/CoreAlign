using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.B2B.PortalComments;

public record OrderCommentPostedPayload(
    Guid OrderId,
    Guid CommentId,
    Guid AuthorUserId,
    string AuthorPersona,
    string Excerpt,
    Guid? OriginDealerAccountId,
    Guid CustomerId);

public interface IOrderCommentPostedOutbox
{
    Task EnqueueAsync(OrderCommentPostedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class OrderCommentPostedOutbox : IOrderCommentPostedOutbox
{
    public const string MessageType = "OrderCommentPosted";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public OrderCommentPostedOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(OrderCommentPostedPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static OrderCommentPostedPayload? Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<OrderCommentPostedPayload>(payloadJson, JsonOptions);
}
