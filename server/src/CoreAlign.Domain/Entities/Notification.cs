using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// In-app notification routed to a single recipient. Created by post-commit
/// outbox handlers (e.g. comment fan-out) so the originating business action
/// is never blocked or rolled back by notification failures.
/// </summary>
public class Notification : TenantEntity
{
    public Guid RecipientUserId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    protected Notification() { }

    public Notification(
        Guid recipientUserId,
        Guid? actorUserId,
        string type,
        string entityType,
        Guid entityId,
        string title,
        string body)
    {
        if (recipientUserId == Guid.Empty) throw new ArgumentException("RecipientUserId is required.", nameof(recipientUserId));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("EntityType is required.", nameof(entityType));
        if (entityId == Guid.Empty) throw new ArgumentException("EntityId is required.", nameof(entityId));

        RecipientUserId = recipientUserId;
        ActorUserId = actorUserId;
        Type = type.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId;
        Title = (title ?? string.Empty).Trim();
        Body = (body ?? string.Empty).Trim();
        IsRead = false;
    }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ReadAtUtc.Value;
    }
}
