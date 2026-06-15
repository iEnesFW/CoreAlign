using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Mrp;

public class MrpPlanRun : TenantEntity, IHasConcurrencyToken
{
    public string Number { get; private set; } = string.Empty;
    public MrpPlanRunStatus Status { get; private set; } = MrpPlanRunStatus.Committed;
    public DateTime AsOfDateUtc { get; private set; }
    public MrpBucketKind BucketKind { get; private set; }
    public int HorizonDays { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;

    public int ProductsEvaluated { get; private set; }
    public int PlannedOrderCount { get; private set; }
    public int ActionMessageCount { get; private set; }

    public Guid CreatedByUserId { get; private set; }
    public DateTime? CommittedAtUtc { get; private set; }

    public long ConcurrencyToken { get; private set; }

    public ICollection<MrpPlannedOrder> PlannedOrders { get; private set; } = new List<MrpPlannedOrder>();
    public ICollection<MrpActionMessage> ActionMessages { get; private set; } = new List<MrpActionMessage>();
    public ICollection<MrpPegging> Peggings { get; private set; } = new List<MrpPegging>();

    protected MrpPlanRun() { }

    public MrpPlanRun(
        string number,
        DateTime asOfDateUtc,
        MrpBucketKind bucketKind,
        int horizonDays,
        Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Plan run number is required.", nameof(number));
        }
        if (horizonDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonDays), "Horizon must be positive.");
        }

        Number = number.Trim();
        AsOfDateUtc = DateTime.SpecifyKind(asOfDateUtc, DateTimeKind.Utc);
        BucketKind = bucketKind;
        HorizonDays = horizonDays;
        CreatedByUserId = createdByUserId;
        IdempotencyKey = BuildIdempotencyKey(AsOfDateUtc, bucketKind, horizonDays);
        Status = MrpPlanRunStatus.Committed;
        CommittedAtUtc = DateTime.UtcNow;
    }

    public static string BuildIdempotencyKey(DateTime asOfDateUtc, MrpBucketKind bucketKind, int horizonDays) =>
        $"{asOfDateUtc:yyyyMMdd}:{bucketKind}:{horizonDays}";

    public void BumpConcurrencyToken() => ConcurrencyToken++;

    public MrpPlannedOrder AddPlannedOrder(MrpPlannedOrder plannedOrder)
    {
        PlannedOrders.Add(plannedOrder);
        PlannedOrderCount = PlannedOrders.Count;
        return plannedOrder;
    }

    public MrpActionMessage AddActionMessage(MrpActionMessage actionMessage)
    {
        ActionMessages.Add(actionMessage);
        ActionMessageCount = ActionMessages.Count;
        return actionMessage;
    }

    public MrpPegging AddPegging(MrpPegging pegging)
    {
        Peggings.Add(pegging);
        return pegging;
    }

    public void SetSummary(int productsEvaluated)
    {
        ProductsEvaluated = productsEvaluated;
        PlannedOrderCount = PlannedOrders.Count;
        ActionMessageCount = ActionMessages.Count;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
