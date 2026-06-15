using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassWorkOrder : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid ProjectId { get; private set; }
    public DateTime ScheduledStartDate { get; private set; }
    public DateTime ScheduledEndDate { get; private set; }
    public Guid? AssignedTeamId { get; private set; }
    public Guid? AssignedInstallerUserId { get; private set; }
    public Guid? MachineId { get; private set; }
    public decimal WorkloadM2 { get; private set; }
    public GlassWorkOrderStatus Status { get; private set; } = GlassWorkOrderStatus.Pending;
    public string ChecklistsJson { get; private set; } = "[]";
    public string? DefectNotes { get; private set; }
    public int RecutCount { get; private set; }

    public string? BomSnapshotJson { get; private set; }
    public decimal? BomSnapshotTotal { get; private set; }
    public Guid? CuttingPlan1DId { get; private set; }
    public Guid? CuttingPlan2DId { get; private set; }
    public int RevisionCount { get; private set; }
    public int RevisionCountAtLastDefect { get; private set; }
    public bool HasOutstandingBlockingRevision { get; private set; }

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

    protected GlassWorkOrder() { }

    public GlassWorkOrder(
        Guid projectId,
        DateTime scheduledStartDate,
        DateTime scheduledEndDate,
        decimal workloadM2,
        Guid? assignedTeamId = null)
    {
        ProjectId = projectId;
        ScheduledStartDate = scheduledStartDate;
        ScheduledEndDate = scheduledEndDate;
        WorkloadM2 = workloadM2;
        AssignedTeamId = assignedTeamId;
        AddDomainEvent(new GlassWorkOrderReleasedEvent(TenantId, Id, projectId, scheduledStartDate, assignedTeamId, DateTime.UtcNow));
    }

    public void Reschedule(DateTime newStart, DateTime newEnd)
    {
        ScheduledStartDate = newStart;
        ScheduledEndDate = newEnd;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void TransitionTo(GlassWorkOrderStatus next)
    {
        if (HasOutstandingBlockingRevision)
            throw new WorkOrderBlockedByRevisionException(Id);

        if (Status == GlassWorkOrderStatus.Defective
            && next != GlassWorkOrderStatus.Defective
            && next != GlassWorkOrderStatus.Installed
            && RevisionCount <= RevisionCountAtLastDefect)
        {
            throw new DefectiveExitRequiresRevisionException(Id);
        }

        if (next == GlassWorkOrderStatus.Defective)
            RevisionCountAtLastDefect = RevisionCount;

        var previous = Status;
        Status = next;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new GlassWorkOrderStatusChangedEvent(TenantId, Id, ProjectId, previous, next, DateTime.UtcNow));

        if (next == GlassWorkOrderStatus.Installed && previous != GlassWorkOrderStatus.Installed)
        {
            var installedAt = DateTime.UtcNow;
            AddDomainEvent(new GlassWorkOrderInstalledEvent(TenantId, Id, ProjectId, installedAt, installedAt));
        }
    }

    public void MarkBlockingRevision()
    {
        HasOutstandingBlockingRevision = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearBlockingRevision()
    {
        HasOutstandingBlockingRevision = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordDefect(string notes)
    {
        DefectNotes = notes;
        RecutCount += 1;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new GlassWorkOrderDefectReportedEvent(TenantId, Id, ProjectId, notes, DateTime.UtcNow));
    }

    public void AssignInstaller(Guid installerUserId)
    {
        AssignedInstallerUserId = installerUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CaptureBomSnapshot(string snapshotJson, decimal grandTotal, Guid? cuttingPlan1DId, Guid? cuttingPlan2DId)
    {
        BomSnapshotJson = snapshotJson;
        BomSnapshotTotal = grandTotal;
        CuttingPlan1DId = cuttingPlan1DId;
        CuttingPlan2DId = cuttingPlan2DId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplySilentSnapshot(string snapshotJson, decimal grandTotal)
    {
        BomSnapshotJson = snapshotJson;
        BomSnapshotTotal = grandTotal;
        RevisionCount += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegisterRevision()
    {
        RevisionCount += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void IncrementRevisionCount()
    {
        RevisionCount += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
