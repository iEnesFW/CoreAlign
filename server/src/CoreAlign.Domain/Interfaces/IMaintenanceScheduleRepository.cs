using CoreAlign.Domain.Entities.Warranty;

namespace CoreAlign.Domain.Interfaces;

public interface IMaintenanceScheduleRepository
{
    Task<MaintenanceSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceSchedule>> ListByWarrantyContractAsync(Guid warrantyContractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceSchedule>> ListDueAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
    Task AddAsync(MaintenanceSchedule schedule, CancellationToken cancellationToken = default);
    void Update(MaintenanceSchedule schedule);
}
