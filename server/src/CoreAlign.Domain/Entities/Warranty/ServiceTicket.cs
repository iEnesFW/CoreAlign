using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.Warranty;

public class ServiceTicket : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid? WarrantyContractId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public ServiceTicketType Type { get; private set; }
    public ServiceTicketStatus Status { get; private set; } = ServiceTicketStatus.Open;
    public ServiceTicketPriority Priority { get; private set; } = ServiceTicketPriority.Normal;
    public string Title { get; private set; } = string.Empty;
    public string DescriptionMd { get; private set; } = string.Empty;
    public DateTime ReportedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionNotesMd { get; private set; }
    public bool IsUnderWarranty { get; private set; }
    public decimal? ChargeableAmount { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    protected ServiceTicket() { }

    public ServiceTicket(
        Guid customerId,
        ServiceTicketType type,
        ServiceTicketPriority priority,
        string title,
        string descriptionMd,
        bool isUnderWarranty,
        Guid? warrantyContractId = null,
        DateTime? reportedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(descriptionMd)) throw new ArgumentException("Description is required.", nameof(descriptionMd));

        CustomerId = customerId;
        Type = type;
        Priority = priority;
        Title = title.Trim();
        DescriptionMd = descriptionMd.Trim();
        WarrantyContractId = warrantyContractId;
        IsUnderWarranty = isUnderWarranty;
        ReportedAtUtc = reportedAtUtc ?? DateTime.UtcNow;
        Status = ServiceTicketStatus.Open;

        AddDomainEvent(new ServiceTicketOpenedEvent(
            TenantId, Id, customerId, warrantyContractId,
            type, priority, Title, isUnderWarranty, DateTime.UtcNow));
    }

    public void Assign(Guid assigneeUserId)
    {
        if (Status is ServiceTicketStatus.Resolved or ServiceTicketStatus.Cancelled)
            throw new InvalidOperationException("Resolved or cancelled tickets cannot be reassigned.");
        AssignedToUserId = assigneeUserId;
        if (Status == ServiceTicketStatus.Open) Status = ServiceTicketStatus.Assigned;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new ServiceTicketAssignedEvent(TenantId, Id, assigneeUserId, DateTime.UtcNow));
    }

    public void StartWork()
    {
        if (Status is ServiceTicketStatus.Resolved or ServiceTicketStatus.Cancelled)
            throw new InvalidOperationException("Cannot start work on a resolved/cancelled ticket.");
        Status = ServiceTicketStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Resolve(string resolutionNotesMd, Guid? workOrderId, decimal? chargeableAmount)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotesMd))
            throw new ArgumentException("Resolution notes are required.", nameof(resolutionNotesMd));
        if (Status == ServiceTicketStatus.Resolved) return;

        Status = ServiceTicketStatus.Resolved;
        ResolutionNotesMd = resolutionNotesMd.Trim();
        WorkOrderId = workOrderId;
        ChargeableAmount = chargeableAmount;
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new ServiceTicketResolvedEvent(TenantId, Id, CustomerId, workOrderId, chargeableAmount, DateTime.UtcNow));
    }

    public void Cancel(string? reason)
    {
        Status = ServiceTicketStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            ResolutionNotesMd = string.IsNullOrWhiteSpace(ResolutionNotesMd)
                ? $"Cancelled: {reason}"
                : $"{ResolutionNotesMd}\nCancelled: {reason}";
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
