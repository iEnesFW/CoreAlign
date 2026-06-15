using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Mrp;

public class MrpPlannedOrder : TenantEntity
{
    public Guid PlanRunId { get; private set; }
    public Guid ProductId { get; private set; }
    public int LowLevelCode { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public DateTime ReleaseDateUtc { get; private set; }
    public Guid? PreferredSupplierId { get; private set; }
    public decimal EstimatedUnitCost { get; private set; }
    public LotSizingPolicy SourcePolicy { get; private set; }
    public bool IsFirmed { get; private set; }
    public bool IsReleased { get; private set; }
    public Guid? ConvertedRequisitionId { get; private set; }
    public decimal? OriginalQuantity { get; private set; }
    public DateTime? OriginalDueDateUtc { get; private set; }

    public bool IsQuantityOverridden => OriginalQuantity is not null;
    public bool IsDueDateOverridden => OriginalDueDateUtc is not null;

    public MrpPlanRun? PlanRun { get; private set; }

    protected MrpPlannedOrder() { }

    public MrpPlannedOrder(
        Guid productId,
        int lowLevelCode,
        decimal quantity,
        DateTime dueDateUtc,
        DateTime releaseDateUtc,
        Guid? preferredSupplierId,
        decimal estimatedUnitCost,
        LotSizingPolicy sourcePolicy)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Planned order quantity must be positive.");
        }
        ProductId = productId;
        LowLevelCode = lowLevelCode;
        Quantity = quantity;
        DueDateUtc = DateTime.SpecifyKind(dueDateUtc, DateTimeKind.Utc);
        ReleaseDateUtc = DateTime.SpecifyKind(releaseDateUtc, DateTimeKind.Utc);
        PreferredSupplierId = preferredSupplierId;
        EstimatedUnitCost = estimatedUnitCost;
        SourcePolicy = sourcePolicy;
    }

    public void Firm(decimal? overrideQuantity, DateTime? overrideDueDateUtc)
    {
        if (IsReleased)
        {
            throw new MrpPlannedOrderAlreadyReleasedException(Id);
        }
        if (overrideQuantity is not null && overrideQuantity.Value != Quantity)
        {
            if (overrideQuantity.Value <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(overrideQuantity), "Override quantity must be positive.");
            }
            OriginalQuantity ??= Quantity;
            Quantity = overrideQuantity.Value;
        }
        if (overrideDueDateUtc is not null)
        {
            var due = DateTime.SpecifyKind(overrideDueDateUtc.Value, DateTimeKind.Utc);
            if (due != DueDateUtc)
            {
                OriginalDueDateUtc ??= DueDateUtc;
                var leadOffset = DueDateUtc - ReleaseDateUtc;
                DueDateUtc = due;
                ReleaseDateUtc = due - leadOffset;
            }
        }
        IsFirmed = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public MrpPlannedOrder CloneFirmedForRun()
    {
        var clone = new MrpPlannedOrder(
            ProductId,
            LowLevelCode,
            Quantity,
            DueDateUtc,
            ReleaseDateUtc,
            PreferredSupplierId,
            EstimatedUnitCost,
            SourcePolicy)
        {
            IsFirmed = true,
            OriginalQuantity = OriginalQuantity,
            OriginalDueDateUtc = OriginalDueDateUtc
        };
        return clone;
    }

    public void MarkReleased(Guid requisitionId)
    {
        if (IsReleased)
        {
            throw new MrpPlannedOrderAlreadyReleasedException(Id);
        }
        IsReleased = true;
        ConvertedRequisitionId = requisitionId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
