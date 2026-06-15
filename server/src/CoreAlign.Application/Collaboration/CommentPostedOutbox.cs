using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Collaboration;

/// <summary>
/// Payload persisted onto the outbox after a comment is created. Drained
/// post-commit by the outbox processor to fan out one Notification per other
/// active tenant user.
/// </summary>
public record CommentPostedPayload(
    Guid CommentId,
    string EntityType,
    Guid EntityId,
    Guid AuthorUserId,
    string Body,
    Guid? ParentCommentId);

/// <summary>
/// Enqueues a comment-posted message onto the transactional outbox so that
/// notification fan-out runs after the comment's business transaction commits.
/// </summary>
public interface ICommentPostedOutbox
{
    Task EnqueueAsync(CommentPostedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class CommentPostedOutbox : ICommentPostedOutbox
{
    public const string MessageType = "CollabCommentPosted";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public CommentPostedOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(CommentPostedPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static CommentPostedPayload? Deserialize(string json)
        => JsonSerializer.Deserialize<CommentPostedPayload>(json, JsonOptions);
}
