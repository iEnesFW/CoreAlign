using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IWarrantyContractRepository
{
    Task<WarrantyContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WarrantyContract?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<WarrantyContract?> GetByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyContract>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyContract>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyContract>> ListAsync(
        WarrantyContractStatus? status,
        Guid? customerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyContract>> ListExpiringWithinDaysAsync(int days, CancellationToken cancellationToken = default);
    Task<int> CountForNumberSequenceAsync(int year, CancellationToken cancellationToken = default);
    Task AddAsync(WarrantyContract contract, CancellationToken cancellationToken = default);
    void Update(WarrantyContract contract);
}
