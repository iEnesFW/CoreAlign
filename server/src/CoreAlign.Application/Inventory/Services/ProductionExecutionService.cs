using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Inventory.Services;

public class ProductionExecutionService : IProductionExecutionService
{
    private readonly IAllocationService _allocation;
    private readonly IProductComponentRepository _components;

    public ProductionExecutionService(
        IAllocationService allocation,
        IProductComponentRepository components)
    {
        _allocation = allocation;
        _components = components;
    }

    public async Task<ProductionExecutionResult> ExecuteAsync(
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0m)
        {
            throw new StockMovementValidationException("Production quantity must be positive.");
        }

        var formula = await _components.GetByParentAsync(productId, cancellationToken);
        if (formula.Count == 0)
        {
            throw new StockMovementValidationException("Cannot produce a product without a defined component formula.");
        }

        decimal totalUnitCost = 0m;
        foreach (var component in formula)
        {
            var issueQty = component.Quantity * quantity;
            var issueMovement = await _allocation.ApplyIssueAsync(new StockIssueRequest(
                ProductId: component.ComponentProductId,
                WarehouseId: warehouseId,
                Quantity: issueQty,
                SourceDocumentType: StockSourceDocumentType.Production,
                SourceDocumentId: null,
                SourceLineId: null,
                SourceReference: reference,
                LotId: null,
                SerialNumber: null,
                ReasonCodeId: null,
                Notes: null), cancellationToken);
            totalUnitCost += issueMovement.UnitCost * component.Quantity;
        }

        await _allocation.ApplyReceiptAsync(new StockReceiptRequest(
            ProductId: productId,
            WarehouseId: warehouseId,
            Quantity: quantity,
            UnitCost: totalUnitCost,
            SourceDocumentType: StockSourceDocumentType.Production,
            SourceDocumentId: null,
            SourceLineId: null,
            SourceReference: reference,
            LotId: null,
            SerialNumber: null,
            ReasonCodeId: null,
            Notes: null), cancellationToken);

        return new ProductionExecutionResult(
            ProductId: productId,
            WarehouseId: warehouseId,
            ProducedQuantity: quantity,
            ComponentsIssued: formula.Count,
            UnitCost: totalUnitCost,
            TotalCost: totalUnitCost * quantity);
    }
}
