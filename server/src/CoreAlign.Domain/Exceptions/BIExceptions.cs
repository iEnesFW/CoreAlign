namespace CoreAlign.Domain.Exceptions;

public sealed class SavedReportNotFoundException : NotFoundException
{
    public SavedReportNotFoundException(Guid id) : base($"Saved report {id} not found.") { }
}

public sealed class DashboardWidgetNotFoundException : NotFoundException
{
    public DashboardWidgetNotFoundException(Guid id) : base($"Dashboard widget {id} not found.") { }
}
