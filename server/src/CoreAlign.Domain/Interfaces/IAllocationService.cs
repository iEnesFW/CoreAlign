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

/// <summary>
/// Outcome of consuming reservations for an order line at dispatch: the quantity
/// actually issued and its Σ issue cost (sum of the issue movements' TotalCost),
/// which the COGS posting relieves from inventory.
/// </summary>
public record OrderLineConsumption(decimal Quantity, decimal Cost);

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
    Guid? PostedByUserId = null,
    StockMovementType? PositiveMovementType = null,
    StockMovementType? NegativeMovementType = null);

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
    Guid? PostedByUserId = null,
    StockMovementType MovementType = StockMovementType.Receipt);

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
    Guid? PostedByUserId = null,
    StockMovementType MovementType = StockMovementType.Issue);

/// <summary>
/// Outcome of an inter-warehouse transfer: the two ledger legs written
/// (<see cref="StockMovementType.TransferOut"/> at source +
/// <see cref="StockMovementType.TransferIn"/> at destination), the source
/// <see cref="StockItem.AvgCost"/> the move was valued at, and the post-move
/// on-hand balances at each warehouse. Globally value-neutral and stock-neutral.
/// </summary>
public record StockTransferResult(
    Guid ProductId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    decimal Quantity,
    decimal UnitCost,
    decimal FromOnHandAfter,
    decimal ToOnHandAfter,
    Guid SourceDocumentId,
    StockMovement TransferOut,
    StockMovement TransferIn,
    int MovementsCreated = 2);

public interface IAllocationService
{
    Task<AllocationResult> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<decimal> ReleaseForOrderLineAsync(Guid orderId, Guid orderLineId, decimal quantity, CancellationToken cancellationToken = default);
    Task<StockMovement> ConsumeAsync(Guid allocationId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default);
    Task<OrderLineConsumption> ConsumeForOrderLineAsync(Guid orderId, Guid orderLineId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default);
    Task<StockMovement> ApplyReceiptAsync(StockReceiptRequest request, CancellationToken cancellationToken = default);
    Task<StockMovement> ApplyIssueAsync(StockIssueRequest request, CancellationToken cancellationToken = default);
    Task<StockMovement> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves <paramref name="quantity"/> of a product between two warehouses in one
    /// transaction: a TransferOut issue at the source (valued at the source
    /// <see cref="StockItem.AvgCost"/>, honouring the no-oversell guard) followed by a
    /// TransferIn receipt at the destination at that same source unit cost. Globally
    /// value- and stock-neutral, so no GL posting. Rejects self-transfer, non-positive
    /// quantity and missing/insufficient source stock (no partial move).
    /// </summary>
    Task<StockTransferResult> ApplyTransferAsync(
        Guid productId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        decimal quantity,
        string? reference = null,
        CancellationToken cancellationToken = default);
}
