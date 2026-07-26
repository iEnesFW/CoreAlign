using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Feedback.Notifications;

public static class FeedbackNotificationKinds
{
    public const string Created = "created";
    public const string StatusChanged = "statusChanged";
    public const string CommentAdded = "commentAdded";
}

public static class FeedbackTemplateKeys
{
    public const string Created = "Feedback.Created";
    public const string StatusChanged = "Feedback.StatusChanged";
    public const string CommentAdded = "Feedback.CommentAdded";
    public const string CategoryKey = "Feedback";

    public static IReadOnlyList<string> All { get; } = [Created, StatusChanged, CommentAdded];
}

// WHY: no DateTime anywhere in this payload. The dispatcher dedups on a SHA256 of the rendered
// payload, so a timestamp would change the hash on every run and re-notify forever.
public record FeedbackNotificationPayload(
    string Kind,
    Guid TenantId,
    Guid TicketId,
    Guid? CreatedByUserId,
    FeedbackType Type,
    FeedbackPriority Priority,
    string Title,
    string? Module,
    FeedbackStatus Status,
    int StatusChangeCount,
    Guid? CommentId,
    string? CommentAuthorName)
{
    public static FeedbackNotificationPayload Created(FeedbackTicket t) => new(
        FeedbackNotificationKinds.Created,
        t.TenantId,
        t.Id,
        t.CreatedByUserId,
        t.Type,
        t.Priority,
        t.Title,
        t.Module,
        t.Status,
        t.StatusChangeCount,
        null,
        t.CreatedByName);

    public static FeedbackNotificationPayload StatusChanged(FeedbackTicket t) => new(
        FeedbackNotificationKinds.StatusChanged,
        t.TenantId,
        t.Id,
        t.CreatedByUserId,
        t.Type,
        t.Priority,
        t.Title,
        t.Module,
        t.Status,
        t.StatusChangeCount,
        null,
        null);

    public static FeedbackNotificationPayload CommentAdded(FeedbackTicket t, FeedbackTicketComment c) => new(
        FeedbackNotificationKinds.CommentAdded,
        t.TenantId,
        t.Id,
        t.CreatedByUserId,
        t.Type,
        t.Priority,
        t.Title,
        t.Module,
        t.Status,
        t.StatusChangeCount,
        c.Id,
        c.AuthorName);
}

public interface IFeedbackNotificationOutbox
{
    Task EnqueueAsync(FeedbackNotificationPayload payload, CancellationToken cancellationToken = default);
}

public sealed class FeedbackNotificationOutbox : IFeedbackNotificationOutbox
{
    public const string MessageType = "FeedbackNotification";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public FeedbackNotificationOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(
        FeedbackNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static FeedbackNotificationPayload? Deserialize(string json)
        => JsonSerializer.Deserialize<FeedbackNotificationPayload>(json, JsonOptions);
}
