namespace CoreAlign.Domain.Enums;

public enum WarrantyCoverageType
{
    ManufacturerDefect = 0,
    Installation = 1,
    FullService = 2,
    Limited = 3
}

public enum WarrantyContractStatus
{
    Active = 0,
    Expired = 1,
    Cancelled = 2,
    Suspended = 3
}

public enum MaintenanceScheduleType
{
    PreventiveAnnual = 0,
    SemiAnnual = 1,
    Quarterly = 2,
    Custom = 99
}

public enum ServiceTicketType
{
    PreventiveMaintenance = 0,
    WarrantyClaim = 1,
    OutOfWarrantyRepair = 2,
    Inspection = 3
}

public enum ServiceTicketStatus
{
    Open = 0,
    Assigned = 1,
    InProgress = 2,
    Resolved = 3,
    Cancelled = 4
}

public enum ServiceTicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
