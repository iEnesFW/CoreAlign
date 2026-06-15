using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Domain.Interfaces;

public interface IReportDefinitionRepository
{
    Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportDefinition>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ReportDefinition definition, CancellationToken cancellationToken = default);
    void Update(ReportDefinition definition);
    void Remove(ReportDefinition definition);
}

public interface IReportScheduleRepository
{
    Task<ReportSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportSchedule>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportSchedule>> GetDueAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task AddAsync(ReportSchedule schedule, CancellationToken cancellationToken = default);
    void Update(ReportSchedule schedule);
    void Remove(ReportSchedule schedule);
}
