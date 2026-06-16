using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface ICustomerUserRepository
{
    Task<CustomerUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerUser?> GetByUserAndCustomerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerUser>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerUser>> ListByTenantAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerUser>> ListActiveByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveOwnershipAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> AnyActiveForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerUser entity, CancellationToken cancellationToken = default);
    void Update(CustomerUser entity);
}

public interface IDealerAccountRepository
{
    Task<DealerAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Batch-load dealer accounts by id (read-only) — one WHERE Id IN (...) instead
    /// of N per-id loops. Missing ids are simply absent from the dictionary.</summary>
    Task<IReadOnlyDictionary<Guid, DealerAccount>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<DealerAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerAccount>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerAccount>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(DealerAccount entity, CancellationToken cancellationToken = default);
    void Update(DealerAccount entity);
}

public interface IDealerUserRepository
{
    Task<DealerUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DealerUser?> GetByUserAndDealerAsync(Guid userId, Guid dealerAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerUser>> ListByDealerAsync(Guid dealerAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerUser>> ListActiveByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveOwnershipAsync(Guid userId, Guid dealerAccountId, CancellationToken cancellationToken = default);
    Task<bool> AnyActiveForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(DealerUser entity, CancellationToken cancellationToken = default);
    void Update(DealerUser entity);
}

public interface IDealerCustomerLinkRepository
{
    Task<DealerCustomerLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DealerCustomerLink?> GetByDealerAndCustomerAsync(Guid dealerAccountId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerCustomerLink>> ListByDealerAsync(Guid dealerAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerCustomerLink>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerCustomerLink>> ListByFilterAsync(Guid? dealerAccountId, Guid? customerId, CancellationToken cancellationToken = default);
    Task AddAsync(DealerCustomerLink entity, CancellationToken cancellationToken = default);
    void Update(DealerCustomerLink entity);
}

public interface IUserMembershipService
{
    Task<UserPersona> ResolvePersonaAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICustomerDealerProductVisibilityRepository
{
    Task<IReadOnlyList<CustomerDealerProductVisibility>> ListByLinkAsync(Guid dealerCustomerLinkId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListVisibleProductIdsAsync(Guid dealerCustomerLinkId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyForLinkAsync(Guid dealerCustomerLinkId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerDealerProductVisibility entity, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(IEnumerable<CustomerDealerProductVisibility> entities, CancellationToken cancellationToken = default);
    Task<CustomerDealerProductVisibility?> GetAsync(Guid dealerCustomerLinkId, Guid productId, CancellationToken cancellationToken = default);
}

public interface IDealerCommissionLedgerRepository
{
    Task<DealerCommissionLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DealerCommissionLedgerEntry?> GetByOrderAndShipmentAsync(
        Guid dealerAccountId,
        Guid orderId,
        Guid? shipmentId,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsForOrderAndShipmentAsync(
        Guid dealerAccountId,
        Guid orderId,
        Guid? shipmentId,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<DealerCommissionLedgerEntry> Items, int Total)> SearchAsync(
        Guid dealerAccountId,
        DealerCommissionStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerCommissionLedgerEntry>> ListForStatementAsync(
        Guid dealerAccountId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
    Task<DealerCommissionSummary> GetSummaryAsync(
        Guid dealerAccountId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
    Task AddAsync(DealerCommissionLedgerEntry entry, CancellationToken cancellationToken = default);
    void Update(DealerCommissionLedgerEntry entry);
}

public record DealerCommissionSummary(
    decimal YtdAccrued,
    decimal YtdPaid,
    decimal ThisMonthAccrued,
    decimal ThisMonthPaid,
    decimal LifetimeAccrued,
    decimal LifetimePaid,
    string Currency);

public interface IB2BAuthorizationService
{
    Task<bool> IsCustomerOwnerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> IsDealerOwnerAsync(Guid userId, Guid dealerAccountId, CancellationToken cancellationToken = default);
    Task<bool> CanManageCustomerAsync(Guid userId, IReadOnlyCollection<string> roles, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> CanManageDealerAsync(Guid userId, IReadOnlyCollection<string> roles, Guid dealerAccountId, CancellationToken cancellationToken = default);
}
