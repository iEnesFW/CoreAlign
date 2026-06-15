using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.BI;

public interface IBIReportService
{
    Task<BIResultDto> ExecuteAsync(BIDataSource dataSource, BIQueryConfigDto config, CancellationToken cancellationToken = default);
    Task<BIResultDto> RunSavedReportAsync(Guid savedReportId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAsync(Guid savedReportId, BIExportFormat format, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<IReadOnlyList<DashboardWidgetDto>> GetUserDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveWidgetLayoutAsync(Guid userId, IReadOnlyList<DashboardWidgetUpsertDto> widgets, CancellationToken cancellationToken = default);
    Task<DashboardWidgetDto> AddWidgetAsync(Guid userId, DashboardWidgetUpsertDto widget, CancellationToken cancellationToken = default);
    Task RemoveWidgetAsync(Guid userId, Guid widgetId, CancellationToken cancellationToken = default);
}

public interface ISavedReportService
{
    Task<IReadOnlyList<SavedReportDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SavedReportDto> CreateAsync(Guid userId, SavedReportUpsertDto dto, CancellationToken cancellationToken = default);
    Task<SavedReportDto> UpdateAsync(Guid userId, Guid id, SavedReportUpsertDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
