namespace CoreAlign.Domain.Entities.Reporting;

public enum ReportEntityType
{
    Invoice = 1,
    Order = 2,
    Customer = 3,
    Product = 4,
    StockMovement = 5,
}

public enum ReportMeasureFunction
{
    Sum = 1,
    Count = 2,
    Avg = 3,
    Min = 4,
    Max = 5,
}

public enum ReportFilterOperator
{
    Equals = 1,
    NotEquals = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LessThan = 5,
    LessThanOrEqual = 6,
    Contains = 7,
    StartsWith = 8,
    In = 9,
    Between = 10,
}

public enum ReportFieldDataType
{
    String = 1,
    Integer = 2,
    Decimal = 3,
    DateTime = 4,
    Boolean = 5,
    Enum = 6,
    Guid = 7,
}

public enum ReportFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Hourly = 4,
    Custom = 5,
}

public enum ReportDeliveryFormat
{
    Pdf = 1,
    Xlsx = 2,
}
