using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IInstallationAcceptanceRepository
{
    Task<InstallationAcceptance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstallationAcceptance?> GetByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<InstallationAcceptance?> GetByAcceptIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallationAcceptance>> ListByInspectorAsync(Guid inspectorUserId, InstallationAcceptanceStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallationAcceptance>> ListPendingAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InstallationAcceptance entity, CancellationToken cancellationToken = default);
    void Update(InstallationAcceptance entity);
}

public interface IPunchListRepository
{
    Task<PunchListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PunchListItem>> ListByAcceptanceAsync(Guid acceptanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PunchListItem>> ListByStatusAsync(PunchListItemStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(PunchListItem entity, CancellationToken cancellationToken = default);
    void Update(PunchListItem entity);
}
