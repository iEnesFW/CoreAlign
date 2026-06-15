namespace CoreAlign.Domain.Enums;

public enum PurchaseRequisitionStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Converted = 4,
    Cancelled = 5,
}

public enum PurchaseRequisitionReason
{
    MRPSuggestion = 0,
    Manual = 1,
    EmergencyOrder = 2,
    StockOut = 3,
}
