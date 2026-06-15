using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Installation;

public class PunchListItem : TenantEntity
{
    public Guid AcceptanceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public PunchListSeverity Severity { get; private set; }
    public PunchListItemStatus Status { get; private set; } = PunchListItemStatus.Open;
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionNotes { get; private set; }

    protected PunchListItem() { }

    public PunchListItem(Guid acceptanceId, string description, PunchListSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description required.", nameof(description));

        AcceptanceId = acceptanceId;
        Description = description;
        Severity = severity;
        Status = PunchListItemStatus.Open;
    }

    public void Assign(Guid userId)
    {
        AssignedToUserId = userId;
        if (Status == PunchListItemStatus.Open)
            Status = PunchListItemStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Resolve(string? resolutionNotes)
    {
        Status = PunchListItemStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Defer()
    {
        Status = PunchListItemStatus.Deferred;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
