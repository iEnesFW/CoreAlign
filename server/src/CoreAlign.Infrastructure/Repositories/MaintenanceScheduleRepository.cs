using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class MaintenanceScheduleRepository : IMaintenanceScheduleRepository
{
    private readonly CoreAlignDbContext _context;
    public MaintenanceScheduleRepository(CoreAlignDbContext context) => _context = context;

    public Task<MaintenanceSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.MaintenanceSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceSchedule>> ListByWarrantyContractAsync(Guid warrantyContractId, CancellationToken cancellationToken = default) =>
        await _context.MaintenanceSchedules
            .AsNoTracking()
            .Where(s => s.WarrantyContractId == warrantyContractId)
            .OrderBy(s => s.NextDueDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MaintenanceSchedule>> ListDueAsync(DateTime asOfDate, CancellationToken cancellationToken = default) =>
        await _context.MaintenanceSchedules
            .AsNoTracking()
            .Where(s => s.IsActive && s.NextDueDate <= asOfDate)
            .OrderBy(s => s.NextDueDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MaintenanceSchedule schedule, CancellationToken cancellationToken = default) =>
        await _context.MaintenanceSchedules.AddAsync(schedule, cancellationToken);

    public void Update(MaintenanceSchedule schedule) => _context.MaintenanceSchedules.Update(schedule);
}
