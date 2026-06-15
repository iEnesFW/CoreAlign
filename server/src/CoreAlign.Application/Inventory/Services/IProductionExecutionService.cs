namespace CoreAlign.Application.Inventory.Services;

public record ProductionExecutionResult(
    Guid ProductId,
    Guid WarehouseId,
    decimal ProducedQuantity,
    int ComponentsIssued,
    decimal UnitCost,
    decimal TotalCost);

/// <summary>
/// Single source of truth for the stock side of "make this product": issue each
/// BOM component (QuantityPer x built quantity) and receive the finished assembly
/// at a rolled-up cost (Sum of component issue cost). Shared by the manual produce
/// command and the MRP production-order completion so the conservation and cost
/// rollup can never diverge.
/// </summary>
public interface IProductionExecutionService
{
    Task<ProductionExecutionResult> ExecuteAsync(
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        string? reference,
        CancellationToken cancellationToken = default);
}
