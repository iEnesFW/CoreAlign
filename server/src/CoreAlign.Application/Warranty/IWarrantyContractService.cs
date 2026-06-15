using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Warranty;

public interface IWarrantyContractService
{
    Task<WarrantyContract> CreateAsync(
        Guid orderId,
        Guid customerId,
        WarrantyCoverageType coverageType,
        int warrantyMonths,
        string termsJson,
        Guid? productId = null,
        Guid? workOrderId = null,
        Guid? invoiceId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid contractId, DateTime startDate, CancellationToken cancellationToken = default);
    Task ExtendAsync(Guid contractId, int monthsAdded, string? reason, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid contractId, string reason, CancellationToken cancellationToken = default);
    Task SuspendAsync(Guid contractId, string? reason, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<bool> CheckValidityAsync(Guid contractId, DateTime asOfDate, CancellationToken cancellationToken = default);
}
