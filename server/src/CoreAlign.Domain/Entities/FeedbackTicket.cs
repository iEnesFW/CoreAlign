using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class FeedbackTicket : TenantEntity, IHasConcurrencyToken
{
    public FeedbackType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public FeedbackPriority Priority { get; private set; }
    public FeedbackStatus Status { get; private set; }
    public string? Module { get; private set; }
    public string? StepsToReproduce { get; private set; }
    public string? PageUrl { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? AdminResponse { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? AttachmentPath { get; private set; }
    public string? AttachmentFileName { get; private set; }
    public string? AttachmentContentType { get; private set; }
    // WHY: the status-changed notification dedups on a hash that must not contain a date, so
    // Open→InProgress→Open→InProgress would hash identically and the repeat would be swallowed.
    // This revision counter is the discriminator.
    public int StatusChangeCount { get; private set; }
    public long ConcurrencyToken { get; private set; }

    public void BumpConcurrencyToken() => ConcurrencyToken += 1;

    protected FeedbackTicket() { }

    public FeedbackTicket(
        FeedbackType type,
        string title,
        string description,
        FeedbackPriority priority,
        string? module = null,
        string? stepsToReproduce = null,
        string? pageUrl = null,
        Guid? createdByUserId = null,
        string? createdByName = null)
    {
        Type = type;
        Title = title;
        Description = description;
        Priority = priority;
        Status = FeedbackStatus.Open;
        Module = module;
        StepsToReproduce = stepsToReproduce;
        PageUrl = pageUrl;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName;
    }

    public void AttachFile(string path, string fileName, string contentType)
    {
        AttachmentPath = path;
        AttachmentFileName = fileName;
        AttachmentContentType = contentType;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetCreatedBy(Guid userId)
    {
        if (userId == Guid.Empty || CreatedByUserId.HasValue) return;
        CreatedByUserId = userId;
    }

    public static bool IsTransitionAllowed(FeedbackStatus from, FeedbackStatus to) =>
        from switch
        {
            FeedbackStatus.Open => to is FeedbackStatus.InProgress
                or FeedbackStatus.Resolved
                or FeedbackStatus.Rejected,
            FeedbackStatus.InProgress => to is FeedbackStatus.Resolved
                or FeedbackStatus.Rejected
                or FeedbackStatus.Open,
            FeedbackStatus.Resolved => to is FeedbackStatus.Closed or FeedbackStatus.InProgress,
            FeedbackStatus.Rejected => to is FeedbackStatus.Open,
            _ => false,
        };

    public bool CanTransitionTo(FeedbackStatus target) => IsTransitionAllowed(Status, target);

    public void ChangeStatus(FeedbackStatus status, string? adminResponse)
    {
        // A repeat of the current status is a no-op, not a conflict — retries and double-clicks must
        // not 409, and must not re-stamp ResolvedAtUtc with a fresh time.
        if (Status == status)
        {
            if (adminResponse is not null) AdminResponse = adminResponse;
            return;
        }
        if (!IsTransitionAllowed(Status, status))
        {
            throw new InvalidFeedbackStatusTransitionException(Status.ToString(), status.ToString());
        }
        Status = status;
        if (adminResponse is not null)
        {
            AdminResponse = adminResponse;
        }
        // WHY: set once, never cleared. Clearing it on reopen erased when the ticket was actually
        // resolved — the one piece of resolution history this aggregate keeps.
        if (status is FeedbackStatus.Resolved or FeedbackStatus.Closed or FeedbackStatus.Rejected)
        {
            ResolvedAtUtc ??= DateTime.UtcNow;
        }
        StatusChangeCount += 1;
        BumpConcurrencyToken();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
