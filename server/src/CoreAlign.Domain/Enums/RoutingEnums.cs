namespace CoreAlign.Domain.Enums;

public enum RoutingStatus
{
    Draft,
    Active,
    Archived,
}

public enum RoutingOperationType
{
    Cutting,
    Edging,
    Tempering,
    Lamination,
    Drilling,
    Sandblasting,
    Washing,
    QualityControl,
    Packaging,
    Other,
}

public enum OperatorQualificationLevel
{
    Trainee,
    Qualified,
    Expert,
}
