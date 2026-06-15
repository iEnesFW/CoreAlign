using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.Installation;

public class InstallationAcceptance : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid WorkOrderId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid CustomerId { get; private set; }
    public InstallationAcceptanceStatus Status { get; private set; } = InstallationAcceptanceStatus.Draft;

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public Guid InspectorUserId { get; private set; }
    public Guid? CustomerSignatureFileId { get; private set; }
    public DateTime? CustomerSignatureCapturedAtUtc { get; private set; }
    public string? CustomerName { get; private set; }

    public string ChecklistJson { get; private set; } = "[]";
    public string PhotoFileIds { get; private set; } = "[]";
    public string? NotesMd { get; private set; }
    public string? RejectionReason { get; private set; }

    public string? AcceptIdempotencyKey { get; private set; }

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

    protected InstallationAcceptance() { }

    public InstallationAcceptance(
        Guid workOrderId,
        Guid projectId,
        Guid customerId,
        Guid inspectorUserId,
        string initialChecklistJson)
    {
        WorkOrderId = workOrderId;
        ProjectId = projectId;
        CustomerId = customerId;
        InspectorUserId = inspectorUserId;
        ChecklistJson = initialChecklistJson;
        Status = InstallationAcceptanceStatus.Draft;
        StartedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new InstallationAcceptanceStartedEvent(
            TenantId, Id, workOrderId, projectId, inspectorUserId, StartedAtUtc, DateTime.UtcNow));
    }

    public void StartInspection(Guid inspectorUserId)
    {
        if (Status != InstallationAcceptanceStatus.Draft)
            throw new InvalidOperationException($"Cannot start inspection in status {Status}.");

        InspectorUserId = inspectorUserId;
        Status = InstallationAcceptanceStatus.InProgress;
        StartedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateChecklist(string checklistJson)
    {
        if (Status is InstallationAcceptanceStatus.Accepted or InstallationAcceptanceStatus.Rejected)
            throw new InvalidOperationException($"Cannot modify checklist in status {Status}.");

        ChecklistJson = checklistJson;
        if (Status == InstallationAcceptanceStatus.Draft)
            Status = InstallationAcceptanceStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddPhoto(string photoFileIdsJson)
    {
        if (Status is InstallationAcceptanceStatus.Accepted or InstallationAcceptanceStatus.Rejected)
            throw new InvalidOperationException($"Cannot add photos in status {Status}.");

        PhotoFileIds = photoFileIdsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CaptureSignature(Guid fileId, string customerName)
    {
        if (Status is InstallationAcceptanceStatus.Accepted or InstallationAcceptanceStatus.Rejected)
            throw new InvalidOperationException($"Cannot capture signature in status {Status}.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name required.", nameof(customerName));

        CustomerSignatureFileId = fileId;
        CustomerSignatureCapturedAtUtc = DateTime.UtcNow;
        CustomerName = customerName;
        Status = InstallationAcceptanceStatus.SignedByCustomer;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new InstallationAcceptanceSignatureCapturedEvent(
            TenantId, Id, fileId, DateTime.UtcNow));
    }

    public void MarkAccepted(string? idempotencyKey = null)
    {
        if (Status != InstallationAcceptanceStatus.SignedByCustomer)
            throw new InvalidOperationException("Customer signature required before accept.");
        if (CustomerSignatureFileId is null)
            throw new InvalidOperationException("Customer signature file missing.");

        Status = InstallationAcceptanceStatus.Accepted;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            AcceptIdempotencyKey = idempotencyKey.Trim();
        }

        AddDomainEvent(new InstallationAcceptedEvent(
            TenantId, Id, WorkOrderId, ProjectId, CustomerId, CompletedAtUtc.Value, DateTime.UtcNow));
    }

    public void MarkRejected(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason required.", nameof(reason));
        if (Status is InstallationAcceptanceStatus.Accepted)
            throw new InvalidOperationException("Already accepted.");

        Status = InstallationAcceptanceStatus.Rejected;
        RejectionReason = reason;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new InstallationRejectedEvent(
            TenantId, Id, WorkOrderId, ProjectId, CustomerId, reason, CompletedAtUtc.Value, DateTime.UtcNow));
    }

    public void SetNotes(string? notesMd)
    {
        NotesMd = notesMd;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
