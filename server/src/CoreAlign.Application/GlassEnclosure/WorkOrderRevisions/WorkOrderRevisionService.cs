using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;

public class WorkOrderRevisionService : IWorkOrderRevisionService
{
    private const decimal SilentThresholdPercent = 5m;
    private const decimal BlockThresholdPercent = 10m;
    private const decimal CumulativeDriftBlockThresholdPercent = 15m;
    private const int ConcurrentInsertMaxAttempts = 2;
    private const string AggregateTypeName = "GlassWorkOrderRevision";

    private readonly IGlassWorkOrderRepository _workOrders;
    private readonly IGlassWorkOrderRevisionRepository _revisions;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAuditContext _auditContext;

    public WorkOrderRevisionService(
        IGlassWorkOrderRepository workOrders,
        IGlassWorkOrderRevisionRepository revisions,
        ICurrentUserAccessor currentUser,
        IAuditContext auditContext)
    {
        _workOrders = workOrders;
        _revisions = revisions;
        _currentUser = currentUser;
        _auditContext = auditContext;
    }

    public async Task<RevisionDecision?> CreateRevisionAsync(
        Guid workOrderId,
        string newSnapshotJson,
        decimal newTotal,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await _workOrders.GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new GlassWorkOrderNotFoundException(workOrderId);

        var previousTotal = workOrder.BomSnapshotTotal ?? 0m;
        var isFirstSnapshot = workOrder.BomSnapshotJson is null;

        var signedDeltaPercent = ComputeSignedDeltaPercent(previousTotal, newTotal, isFirstSnapshot);
        var absDeltaPercent = Math.Abs(signedDeltaPercent);

        var snapshotsIdentical = !isFirstSnapshot
            && BomSnapshotJsonBuilder.SnapshotsEqual(workOrder.BomSnapshotJson, newSnapshotJson)
            && previousTotal == newTotal;

        if (snapshotsIdentical)
        {
            return null;
        }

        var status = ResolveStatus(absDeltaPercent, previousTotal, newTotal, isFirstSnapshot);

        if (status == WorkOrderRevisionStatus.SilentSnapshot)
        {
            var cumulativeDrift = await _revisions.GetCumulativeSignedDeltaSinceLastApprovalAsync(workOrderId, cancellationToken);
            var projectedCumulative = cumulativeDrift + signedDeltaPercent;
            if (Math.Abs(projectedCumulative) > CumulativeDriftBlockThresholdPercent)
            {
                status = WorkOrderRevisionStatus.PendingApproval;
            }
        }

        var deltaJson = JsonSerializer.Serialize(new
        {
            previousTotal,
            newTotal,
            deltaPercent = signedDeltaPercent,
            absPercent = absDeltaPercent,
            decision = status.ToString()
        });

        var createdByUserId = _currentUser.UserIdOrThrow();

        for (var attempt = 0; attempt < ConcurrentInsertMaxAttempts; attempt++)
        {
            var nextNumber = (await _revisions.GetMaxRevisionNumberAsync(workOrderId, cancellationToken)) + 1;
            var revision = new GlassWorkOrderRevision(
                workOrder.Id,
                nextNumber,
                workOrder.BomSnapshotJson,
                newSnapshotJson,
                deltaJson,
                signedDeltaPercent,
                reason,
                status,
                createdByUserId)
            {
                TenantId = workOrder.TenantId
            };

            try
            {
                await _revisions.AddAsync(revision, cancellationToken);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                if (attempt == ConcurrentInsertMaxAttempts - 1)
                {
                    throw new ConcurrentRevisionInsertException(workOrderId, nextNumber);
                }
                continue;
            }

            ApplyDecisionToWorkOrder(workOrder, status, newSnapshotJson, newTotal);
            _workOrders.Update(workOrder);

            return new RevisionDecision(revision.Id, nextNumber, status, signedDeltaPercent);
        }

        throw new ConcurrentRevisionInsertException(workOrderId, 0);
    }

    private static bool IsUniqueViolation(Exception ex)
    {
        Exception? current = ex;
        while (current is not null)
        {
            if (current.GetType().Name == "PostgresException")
            {
                var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
                if (sqlState == "23505")
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(current.Message)
                && current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    public async Task ApproveRevisionAsync(Guid revisionId, string? overrideReason, CancellationToken cancellationToken = default)
    {
        var revision = await _revisions.GetByIdAsync(revisionId, cancellationToken)
            ?? throw new GlassWorkOrderRevisionNotFoundException(revisionId);

        var utcNow = DateTime.UtcNow;
        var userId = _currentUser.UserIdOrThrow();
        var statusBefore = revision.Status;

        if (statusBefore == WorkOrderRevisionStatus.Blocked)
        {
            if (string.IsNullOrWhiteSpace(overrideReason))
            {
                throw new InvalidOperationException("Override reason is required to approve a blocked revision.");
            }
            revision.OverrideBlock(userId, overrideReason!, utcNow);
        }
        else
        {
            revision.Approve(userId, utcNow);
        }

        _revisions.Update(revision);

        var workOrder = await _workOrders.GetByIdAsync(revision.WorkOrderId, cancellationToken);
        if (workOrder is null)
        {
            CaptureApprovalAudit(revision, statusBefore, userId, overrideReason);
            return;
        }

        var newTotal = RequireNewTotal(revision.DeltaJson, revision.Id);
        workOrder.CaptureBomSnapshot(
            revision.NewSnapshotJson,
            newTotal,
            workOrder.CuttingPlan1DId,
            workOrder.CuttingPlan2DId);
        workOrder.RegisterRevision();

        if (statusBefore == WorkOrderRevisionStatus.Blocked)
        {
            var hasOther = await _revisions.AnyOutstandingBlockingAsync(revision.WorkOrderId, revision.Id, cancellationToken);
            if (!hasOther)
            {
                var refreshed = await _workOrders.GetByIdAsync(revision.WorkOrderId, cancellationToken);
                if (refreshed is not null && refreshed.HasOutstandingBlockingRevision)
                {
                    var hasOtherAfterRefresh = await _revisions.AnyOutstandingBlockingAsync(
                        revision.WorkOrderId, revision.Id, cancellationToken);
                    if (!hasOtherAfterRefresh)
                    {
                        workOrder.ClearBlockingRevision();
                    }
                }
            }
        }

        _workOrders.Update(workOrder);

        CaptureApprovalAudit(revision, statusBefore, userId, overrideReason);
    }

    public async Task RejectRevisionAsync(Guid revisionId, string reason, CancellationToken cancellationToken = default)
    {
        var revision = await _revisions.GetByIdAsync(revisionId, cancellationToken)
            ?? throw new GlassWorkOrderRevisionNotFoundException(revisionId);

        var statusBefore = revision.Status;
        var userId = _currentUser.UserIdOrThrow();
        revision.Reject(userId, reason, DateTime.UtcNow);
        _revisions.Update(revision);

        var workOrder = await _workOrders.GetByIdAsync(revision.WorkOrderId, cancellationToken);
        if (workOrder is null)
        {
            CaptureRejectionAudit(revision, statusBefore, userId, reason);
            return;
        }

        if (statusBefore == WorkOrderRevisionStatus.Blocked)
        {
            var hasOther = await _revisions.AnyOutstandingBlockingAsync(revision.WorkOrderId, revision.Id, cancellationToken);
            if (!hasOther)
            {
                var refreshed = await _workOrders.GetByIdAsync(revision.WorkOrderId, cancellationToken);
                if (refreshed is not null && refreshed.HasOutstandingBlockingRevision)
                {
                    var hasOtherAfterRefresh = await _revisions.AnyOutstandingBlockingAsync(
                        revision.WorkOrderId, revision.Id, cancellationToken);
                    if (!hasOtherAfterRefresh)
                    {
                        workOrder.ClearBlockingRevision();
                        _workOrders.Update(workOrder);
                    }
                }
            }
        }

        CaptureRejectionAudit(revision, statusBefore, userId, reason);
    }

    private void CaptureApprovalAudit(
        GlassWorkOrderRevision revision,
        WorkOrderRevisionStatus statusBefore,
        Guid userId,
        string? overrideReason)
    {
        var isOverride = statusBefore == WorkOrderRevisionStatus.Blocked;
        var action = isOverride ? "WorkOrderRevision.BlockOverridden" : "WorkOrderRevision.Approved";
        var payload = JsonSerializer.Serialize(new
        {
            revisionId = revision.Id,
            revisionNumber = revision.RevisionNumber,
            workOrderId = revision.WorkOrderId,
            statusBefore = statusBefore.ToString(),
            statusAfter = revision.Status.ToString(),
            deltaPercent = revision.DeltaPercent,
            actor = userId,
            overrideReason
        });
        _auditContext.CaptureCustom(revision.Id, AggregateTypeName, action, payload);
    }

    private void CaptureRejectionAudit(
        GlassWorkOrderRevision revision,
        WorkOrderRevisionStatus statusBefore,
        Guid userId,
        string reason)
    {
        var payload = JsonSerializer.Serialize(new
        {
            revisionId = revision.Id,
            revisionNumber = revision.RevisionNumber,
            workOrderId = revision.WorkOrderId,
            statusBefore = statusBefore.ToString(),
            statusAfter = revision.Status.ToString(),
            deltaPercent = revision.DeltaPercent,
            actor = userId,
            rejectionReason = reason
        });
        _auditContext.CaptureCustom(revision.Id, AggregateTypeName, "WorkOrderRevision.Rejected", payload);
    }

    private static decimal ComputeSignedDeltaPercent(decimal previousTotal, decimal newTotal, bool isFirstSnapshot)
    {
        if (previousTotal == 0m)
        {
            if (isFirstSnapshot)
            {
                return 0m;
            }
            return newTotal == 0m ? 0m : decimal.MaxValue;
        }

        return Math.Round((newTotal - previousTotal) / previousTotal * 100m, 2);
    }

    private static WorkOrderRevisionStatus ResolveStatus(
        decimal absDeltaPercent,
        decimal previousTotal,
        decimal newTotal,
        bool isFirstSnapshot)
    {
        if (previousTotal == 0m && !isFirstSnapshot && newTotal != 0m)
        {
            return WorkOrderRevisionStatus.Blocked;
        }

        if (absDeltaPercent < SilentThresholdPercent)
        {
            return WorkOrderRevisionStatus.SilentSnapshot;
        }

        return absDeltaPercent <= BlockThresholdPercent
            ? WorkOrderRevisionStatus.PendingApproval
            : WorkOrderRevisionStatus.Blocked;
    }

    private static void ApplyDecisionToWorkOrder(
        GlassWorkOrder workOrder,
        WorkOrderRevisionStatus status,
        string newSnapshotJson,
        decimal newTotal)
    {
        switch (status)
        {
            case WorkOrderRevisionStatus.SilentSnapshot:
                workOrder.ApplySilentSnapshot(newSnapshotJson, newTotal);
                workOrder.ClearBlockingRevision();
                break;
            case WorkOrderRevisionStatus.Blocked:
                workOrder.MarkBlockingRevision();
                break;
            case WorkOrderRevisionStatus.PendingApproval:
                break;
        }
    }

    private static decimal RequireNewTotal(string? deltaJson, Guid revisionId)
    {
        if (string.IsNullOrWhiteSpace(deltaJson))
        {
            throw new InvalidOperationException($"Revision {revisionId} has no delta payload to extract newTotal from.");
        }

        try
        {
            var node = JsonSerializer.Deserialize<JsonElement>(deltaJson);
            if (!node.TryGetProperty("newTotal", out var t))
            {
                throw new InvalidOperationException($"Revision {revisionId} delta payload missing newTotal.");
            }
            return t.GetDecimal();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Revision {revisionId} delta payload is malformed: {ex.Message}", ex);
        }
    }
}
