using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IGLPostingMappingRepository
{
    Task<IReadOnlyList<GLPostingMapping>> ListAsync(CancellationToken cancellationToken = default);
    Task<GLPostingMapping?> GetByKeyAsync(GLPostingKey postingKey, CancellationToken cancellationToken = default);
    Task AddAsync(GLPostingMapping mapping, CancellationToken cancellationToken = default);
    void Update(GLPostingMapping mapping);
}
