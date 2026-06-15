using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Collaboration;

/// <summary>
/// Drains a CollabCommentPosted outbox message into per-recipient Notification
/// rows. Runs post-commit on the producing request's pipeline, so the comment
/// is durably saved before any fan-out attempt and a failure here never
/// rolls back the comment itself.
/// </summary>
public sealed class CommentPostedOutboxHandler : IOutboxMessageHandler
{
    private const int BodyPreviewLength = 200;

    public string MessageType => CommentPostedOutbox.MessageType;

    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public CommentPostedOutboxHandler(
        INotificationRepository notifications,
        IUserRepository users,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _notifications = notifications;
        _users = users;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        CommentPostedPayload? payload;
        try
        {
            payload = CommentPostedOutbox.Deserialize(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null)
        {
            return OutboxHandlerResult.Failed("Payload deserialized to null.");
        }

        var tenantId = _tenant.CurrentTenantId;
        if (tenantId is null)
        {
            return OutboxHandlerResult.Failed("Tenant context missing during drain.");
        }

        // Fan out to every OTHER active tenant user. Single user (the author) → no recipients → nothing to do.
        var tenantUsers = await _users.ListByTenantAsync(tenantId.Value, cancellationToken);
        var recipients = tenantUsers
            .Where(u => u.IsActive && u.Id != payload.AuthorUserId)
            .ToList();
        if (recipients.Count == 0)
        {
            return OutboxHandlerResult.Processed("NoRecipients");
        }

        var author = tenantUsers.FirstOrDefault(u => u.Id == payload.AuthorUserId);
        var actorName = author is null ? "Someone" : CollaborationMapper.DisplayNameFor(author);

        var title = payload.ParentCommentId.HasValue
            ? $"{actorName} replied on {payload.EntityType}"
            : $"{actorName} commented on {payload.EntityType}";

        var body = payload.Body.Length > BodyPreviewLength
            ? payload.Body[..BodyPreviewLength] + "..."
            : payload.Body;

        var notifications = recipients.Select(u => new Notification(
            u.Id,
            payload.AuthorUserId,
            "CommentPosted",
            payload.EntityType,
            payload.EntityId,
            title,
            body)).ToList();

        await _notifications.AddRangeAsync(notifications, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return OutboxHandlerResult.Processed($"FannedOut:{notifications.Count}");
    }
}
