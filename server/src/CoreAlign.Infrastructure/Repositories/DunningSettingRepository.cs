using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class DunningSettingRepository : IDunningSettingRepository
{
    private readonly CoreAlignDbContext _context;
    public DunningSettingRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<DunningSetting>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.DunningSettings.AsNoTracking().OrderBy(d => d.Type).ToListAsync(cancellationToken);

    public Task<DunningSetting?> GetByTypeAsync(DunningType type, CancellationToken cancellationToken = default) =>
        _context.DunningSettings.FirstOrDefaultAsync(d => d.Type == type, cancellationToken);

    public async Task AddAsync(DunningSetting setting, CancellationToken cancellationToken = default) =>
        await _context.DunningSettings.AddAsync(setting, cancellationToken);

    public void Update(DunningSetting setting) => _context.DunningSettings.Update(setting);
}
