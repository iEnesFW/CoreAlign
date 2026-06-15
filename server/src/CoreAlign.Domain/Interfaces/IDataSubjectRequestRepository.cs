using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IDataSubjectRequestRepository
{
    Task<DataSubjectRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataSubjectRequest>> ListAsync(
        DataSubjectRequestStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(DataSubjectRequestStatus? status, CancellationToken cancellationToken = default);

    Task AddAsync(DataSubjectRequest entity, CancellationToken cancellationToken = default);

    void Update(DataSubjectRequest entity);
}
