using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class FeedbackTicket : TenantEntity
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

    public void ChangeStatus(FeedbackStatus status, string? adminResponse)
    {
        Status = status;
        if (adminResponse is not null)
        {
            AdminResponse = adminResponse;
        }
        ResolvedAtUtc = status is FeedbackStatus.Resolved or FeedbackStatus.Closed or FeedbackStatus.Rejected
            ? DateTime.UtcNow
            : null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
