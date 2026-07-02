using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IDunningSettingRepository
{
    Task<IReadOnlyList<DunningSetting>> ListAsync(CancellationToken cancellationToken = default);
    Task<DunningSetting?> GetByTypeAsync(DunningType type, CancellationToken cancellationToken = default);
    Task AddAsync(DunningSetting setting, CancellationToken cancellationToken = default);
    void Update(DunningSetting setting);
}
