namespace CoreAlign.Domain.Entities.Reporting;

public enum DashboardWidgetType
{
    LineChart = 1,
    BarChart = 2,
    StatCard = 3,
    Table = 4,
    Calendar = 5,
    PieChart = 6,
    AreaChart = 7,
}

public enum BIDataSource
{
    Sales = 1,
    Inventory = 2,
    Warranty = 3,
    Service = 4,
    Cash = 5,
    AR = 6,
    AP = 7,
}

public enum BIExportFormat
{
    Pdf = 1,
    Xlsx = 2,
    Csv = 3,
}
