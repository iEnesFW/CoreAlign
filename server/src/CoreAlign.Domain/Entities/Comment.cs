using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Collaboration comment attached to a parent business record (Order, VendorBill,
/// Shipment). Top-level when <see cref="ParentCommentId"/> is null; a reply when
/// it references another comment in the same entity scope. One-level threading
/// only — replies cannot themselves be replied to.
/// </summary>
public class Comment : TenantEntity
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public Guid? ParentCommentId { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }

    protected Comment() { }

    public Comment(string entityType, Guid entityId, Guid authorUserId, string body, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("EntityType is required.", nameof(entityType));
        if (entityId == Guid.Empty) throw new ArgumentException("EntityId is required.", nameof(entityId));
        if (authorUserId == Guid.Empty) throw new ArgumentException("AuthorUserId is required.", nameof(authorUserId));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));

        EntityType = entityType.Trim();
        EntityId = entityId;
        AuthorUserId = authorUserId;
        Body = body.Trim();
        ParentCommentId = parentCommentId;
    }

    public void Edit(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));
        Body = body.Trim();
        EditedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = EditedAtUtc.Value;
    }
}
