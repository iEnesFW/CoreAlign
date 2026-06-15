using CoreAlign.Domain.Entities.Manufacturing;

namespace CoreAlign.Domain.Interfaces;

public interface IWorkCenterRepository
{
    Task AddAsync(WorkCenter workCenter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkCenter>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<WorkCenter?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
