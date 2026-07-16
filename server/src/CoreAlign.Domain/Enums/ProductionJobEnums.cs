namespace CoreAlign.Domain.Enums;

public enum ProductionJobStatus
{
    Draft,
    Released,
    InProgress,
    OnHold,
    ReadyToComplete,
    Completed,
    Cancelled,
}

public enum ProductionJobStepStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped,
    Reopened,
}
