using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public record AllocationRequest(
    Guid OrderId,
    Guid OrderLineId,
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    Guid? LotId = null);

public record AllocationResult(StockAllocation Allocation, StockItem StockItem);

public record StockAdjustmentRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal Delta,
    decimal? UnitCost,
    StockSourceDocumentType SourceDocumentType,
    Guid? SourceDocumentId,
    Guid? ReasonCodeId,
    string? Notes,
    Guid? LotId = null,
    Guid? PostedByUserId = null);

public record StockReceiptRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    decimal UnitCost,
    StockSourceDocumentType SourceDocumentType,
    Guid? SourceDocumentId,
    Guid? SourceLineId,
    string? SourceReference,
    Guid? LotId,
    string? SerialNumber,
    Guid? ReasonCodeId,
    string? Notes,
    Guid? PostedByUserId = null);

public record StockIssueRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    StockSourceDocumentType SourceDocumentType,
    Guid? SourceDocumentId,
    Guid? SourceLineId,
    string? SourceReference,
    Guid? LotId,
    string? SerialNumber,
    Guid? ReasonCodeId,
    string? Notes,
    Guid? PostedByUserId = null);

public interface IAllocationService
{
    Task<AllocationResult> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<StockMovement> ConsumeAsync(Guid allocationId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default);
    Task<decimal> ConsumeForOrderLineAsync(Guid orderId, Guid orderLineId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default);
    Task<StockMovement> ApplyReceiptAsync(StockReceiptRequest request, CancellationToken cancellationToken = default);
    Task<StockMovement> ApplyIssueAsync(StockIssueRequest request, CancellationToken cancellationToken = default);
    Task<StockMovement> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default);
}
