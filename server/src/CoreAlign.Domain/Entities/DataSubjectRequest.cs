using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public enum DataSubjectRequestType
{
    Export = 0,
    Erasure = 1,
    Access = 2,
    Rectification = 3,
    Portability = 4,
    Restriction = 5,
    Objection = 6,
}

public enum DataSubjectRequestStatus
{
    Submitted = 0,
    InProgress = 1,
    Completed = 2,
    Rejected = 3,
}

public enum LegalBasisOverride
{
    None = 0,
    Consent = 1,
    Contract = 2,
    LegalObligation = 3,
    VitalInterest = 4,
    PublicTask = 5,
    LegitimateInterest = 6,
}

public class DataSubjectRequest : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid UserId { get; private set; }
    public DataSubjectRequestType RequestType { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public string? UsernameHash { get; private set; }
    public string? EmailHash { get; private set; }

    public Guid? RequesterUserId { get; private set; }
    public Guid? RequesterCustomerId { get; private set; }
    public DataSubjectRequestStatus Status { get; private set; } = DataSubjectRequestStatus.Submitted;
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? DataExportFileId { get; private set; }
    public LegalBasisOverride LegalBasisOverride { get; private set; } = LegalBasisOverride.None;
    public string? Notes { get; private set; }

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

    protected DataSubjectRequest() { }

    public DataSubjectRequest(
        Guid tenantId,
        Guid userId,
        DataSubjectRequestType requestType,
        DateTime requestedAtUtc,
        string? usernameHash,
        string? emailHash)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        UserId = userId;
        RequestType = requestType;
        RequestedAtUtc = requestedAtUtc;
        SubmittedAtUtc = requestedAtUtc;
        UsernameHash = usernameHash;
        EmailHash = emailHash;
        Status = DataSubjectRequestStatus.Completed;
        CompletedAtUtc = requestedAtUtc;
    }

    public static DataSubjectRequest Submit(
        Guid tenantId,
        DataSubjectRequestType requestType,
        DateTime submittedAtUtc,
        Guid? requesterUserId,
        Guid? requesterCustomerId,
        string? usernameHash,
        string? emailHash,
        string? notes)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));

        return new DataSubjectRequest
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = requesterUserId ?? Guid.Empty,
            RequestType = requestType,
            RequestedAtUtc = submittedAtUtc,
            SubmittedAtUtc = submittedAtUtc,
            Status = DataSubjectRequestStatus.Submitted,
            RequesterUserId = requesterUserId,
            RequesterCustomerId = requesterCustomerId,
            UsernameHash = usernameHash,
            EmailHash = emailHash,
            Notes = notes,
        };
    }

    public void MarkInProgress(DateTime utcNow)
    {
        Status = DataSubjectRequestStatus.InProgress;
        UpdatedAtUtc = utcNow;
    }

    public void MarkCompleted(DateTime utcNow, Guid? exportFileId = null)
    {
        Status = DataSubjectRequestStatus.Completed;
        CompletedAtUtc = utcNow;
        if (exportFileId.HasValue) DataExportFileId = exportFileId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkRejected(DateTime utcNow, string reason)
    {
        Status = DataSubjectRequestStatus.Rejected;
        RejectionReason = reason;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void SetLegalBasis(LegalBasisOverride basis, DateTime utcNow)
    {
        LegalBasisOverride = basis;
        UpdatedAtUtc = utcNow;
    }
}
