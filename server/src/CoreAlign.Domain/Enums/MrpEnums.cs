namespace CoreAlign.Domain.Enums;

public enum LotSizingPolicy
{
    LotForLot = 0,
    FixedOrderQuantity = 1,
    MinMax = 2,
    EconomicOrderQuantity = 3,
    PeriodOrderQuantity = 4
}

public enum MrpBucketKind
{
    Day = 0,
    Week = 1
}

public enum MrpActionType
{
    Release = 0,
    RescheduleIn = 1,
    RescheduleOut = 2,
    Expedite = 3,
    CancelSupply = 4,
    BelowSafetyStock = 5,
    ProjectedStockout = 6
}

public enum MrpActionSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum MrpPlanRunStatus
{
    Preview = 0,
    Committed = 1
}

public enum ForecastModel
{
    MovingAverage = 0,
    ExponentialSmoothing = 1,
    HoltLinear = 2,
    HoltWinters = 3
}

public enum ProcurementType
{
    Buy = 0,
    Make = 1
}

public enum PlannedProductionOrderStatus
{
    Planned = 0,
    Firm = 1,
    Released = 2,
    Closed = 3
}

public enum MrpPlanningMode
{
    Regenerative = 0,
    NetChange = 1
}

public enum AbcClass
{
    Unclassified = 0,
    A = 1,
    B = 2,
    C = 3
}
