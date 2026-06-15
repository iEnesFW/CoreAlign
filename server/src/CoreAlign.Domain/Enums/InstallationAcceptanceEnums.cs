namespace CoreAlign.Domain.Enums;

public enum InstallationAcceptanceStatus
{
    Draft = 0,
    InProgress = 1,
    SignedByCustomer = 2,
    Accepted = 3,
    Rejected = 4
}

public enum InstallationChecklistResult
{
    NotEvaluated = 0,
    Pass = 1,
    Fail = 2,
    NotApplicable = 3
}

public enum PunchListSeverity
{
    Minor = 0,
    Moderate = 1,
    Critical = 2
}

public enum PunchListItemStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Deferred = 3
}
