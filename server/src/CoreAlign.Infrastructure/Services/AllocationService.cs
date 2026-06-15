using System.Linq;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

public class AllocationService : IAllocationService
{
    private readonly IStockItemRepository _stockItems;
    private readonly IStockMovementRepository _movements;
    private readonly IStockAllocationRepository _allocations;
    private readonly IWarehouseRepository _warehouses;
    private readonly IProductRepository _products;
    private readonly IStockOpeningBalanceBridge _openingBalance;

    public AllocationService(
        IStockItemRepository stockItems,
        IStockMovementRepository movements,
        IStockAllocationRepository allocations,
        IWarehouseRepository warehouses,
        IProductRepository products,
        IStockOpeningBalanceBridge openingBalance)
    {
        _stockItems = stockItems;
        _movements = movements;
        _allocations = allocations;
        _warehouses = warehouses;
        _products = products;
        _openingBalance = openingBalance;
    }

    public async Task<AllocationResult> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _stockItems.GetOrCreateAsync(request.ProductId, request.WarehouseId, request.LotId, cancellationToken);
        await _openingBalance.EnsureMaterializedAsync(item, cancellationToken);
        var now = DateTime.UtcNow;

        item.Reserve(request.Quantity, now);

        var allocation = new StockAllocation(
            orderId: request.OrderId,
            orderLineId: request.OrderLineId,
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            quantity: request.Quantity,
            lotId: request.LotId);
        await _allocations.AddAsync(allocation, cancellationToken);

        await _movements.AddAsync(new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: StockMovementType.Reservation,
            quantity: request.Quantity,
            unitCost: item.AvgCost,
            onHandAfter: item.OnHand,
            avgCostAfter: item.AvgCost,
            occurredAtUtc: now,
            sourceDocumentType: StockSourceDocumentType.Order,
            sourceDocumentId: request.OrderId,
            sourceLineId: request.OrderLineId,
            lotId: request.LotId,
            notes: "Reserved for order"
        ), cancellationToken);

        return new AllocationResult(allocation, item);
    }

    public async Task ReleaseAsync(Guid allocationId, CancellationToken cancellationToken = default)
    {
        var allocation = await _allocations.GetByIdAsync(allocationId, cancellationToken)
            ?? throw new AllocationNotFoundException(allocationId);
        if (allocation.Status == AllocationStatus.Released || allocation.Status == AllocationStatus.Consumed) return;

        var item = await _stockItems.GetAsync(allocation.ProductId, allocation.WarehouseId, allocation.LotId, cancellationToken);
        var now = DateTime.UtcNow;
        var remaining = allocation.Remaining;
        if (item is not null && remaining > 0m)
        {
            item.Release(remaining, now);
            await _movements.AddAsync(new StockMovement(
                productId: allocation.ProductId,
                warehouseId: allocation.WarehouseId,
                type: StockMovementType.UnReservation,
                quantity: remaining,
                unitCost: item.AvgCost,
                onHandAfter: item.OnHand,
                avgCostAfter: item.AvgCost,
                occurredAtUtc: now,
                sourceDocumentType: StockSourceDocumentType.Order,
                sourceDocumentId: allocation.OrderId,
                sourceLineId: allocation.OrderLineId,
                lotId: allocation.LotId,
                notes: "Allocation released"
            ), cancellationToken);
        }
        allocation.Release(now);
        _allocations.Update(allocation);
    }

    public async Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var allocations = await _allocations.GetByOrderAsync(orderId, cancellationToken);
        foreach (var a in allocations.Where(a => a.Status == AllocationStatus.Active || a.Status == AllocationStatus.PartiallyConsumed))
        {
            await ReleaseAsync(a.Id, cancellationToken);
        }
    }

    public async Task<StockMovement> ConsumeAsync(Guid allocationId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default)
    {
        var allocation = await _allocations.GetByIdAsync(allocationId, cancellationToken)
            ?? throw new AllocationNotFoundException(allocationId);
        var item = await _stockItems.GetAsync(allocation.ProductId, allocation.WarehouseId, allocation.LotId, cancellationToken)
            ?? throw new StockMovementValidationException("StockItem missing for allocation consumption.");

        var consumeQty = Math.Min(quantity, allocation.Remaining);
        if (consumeQty <= 0m)
        {
            throw new StockMovementValidationException("No remaining quantity to consume on allocation.");
        }
        var now = DateTime.UtcNow;
        item.ConsumeReservation(consumeQty, now);
        allocation.Consume(consumeQty, now);

        var product = await _products.GetByIdAsync(allocation.ProductId, cancellationToken);
        if (product is not null && product.IsStockTracked)
        {
            product.AdjustStock(-consumeQty);
            _products.Update(product);
        }

        var movement = new StockMovement(
            productId: allocation.ProductId,
            warehouseId: allocation.WarehouseId,
            type: StockMovementType.Issue,
            quantity: consumeQty,
            unitCost: item.AvgCost,
            onHandAfter: item.OnHand,
            avgCostAfter: item.AvgCost,
            occurredAtUtc: now,
            sourceDocumentType: StockSourceDocumentType.Order,
            sourceDocumentId: allocation.OrderId,
            sourceLineId: allocation.OrderLineId,
            lotId: allocation.LotId,
            postedByUserId: postedByUserId,
            notes: "Reservation consumed (shipment)");
        await _movements.AddAsync(movement, cancellationToken);
        _allocations.Update(allocation);
        return movement;
    }

    public async Task<OrderLineConsumption> ConsumeForOrderLineAsync(Guid orderId, Guid orderLineId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0m) return new OrderLineConsumption(0m, 0m);

        var allocations = await _allocations.GetByOrderAsync(orderId, cancellationToken);
        var pending = allocations
            .Where(a => a.OrderLineId == orderLineId
                && (a.Status == AllocationStatus.Active || a.Status == AllocationStatus.PartiallyConsumed))
            .ToList();

        var remaining = quantity;
        var consumed = 0m;
        var cost = 0m;
        foreach (var allocation in pending)
        {
            if (remaining <= 0m) break;
            var take = Math.Min(remaining, allocation.Remaining);
            if (take <= 0m) continue;
            var movement = await ConsumeAsync(allocation.Id, take, postedByUserId, cancellationToken);
            remaining -= take;
            consumed += take;
            cost += movement.TotalCost;
        }

        return new OrderLineConsumption(consumed, cost);
    }

    public async Task<StockMovement> ApplyReceiptAsync(StockReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0m)
        {
            throw new StockMovementValidationException("Receipt quantity must be positive.");
        }
        var item = await _stockItems.GetOrCreateAsync(request.ProductId, request.WarehouseId, request.LotId, cancellationToken);
        var now = DateTime.UtcNow;
        item.ApplyReceipt(request.Quantity, request.UnitCost, now);
        await SyncProductStockAsync(request.ProductId, request.Quantity, cancellationToken);

        var movement = new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: request.MovementType,
            quantity: request.Quantity,
            unitCost: request.UnitCost,
            onHandAfter: item.OnHand,
            avgCostAfter: item.AvgCost,
            occurredAtUtc: now,
            sourceDocumentType: request.SourceDocumentType,
            sourceDocumentId: request.SourceDocumentId,
            sourceLineId: request.SourceLineId,
            sourceReference: request.SourceReference,
            lotId: request.LotId,
            serialNumber: request.SerialNumber,
            reasonCodeId: request.ReasonCodeId,
            postedByUserId: request.PostedByUserId,
            notes: request.Notes);
        await _movements.AddAsync(movement, cancellationToken);
        return movement;
    }

    public async Task<StockMovement> ApplyIssueAsync(StockIssueRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0m)
        {
            throw new StockMovementValidationException("Issue quantity must be positive.");
        }
        var item = await _stockItems.GetAsync(request.ProductId, request.WarehouseId, request.LotId, cancellationToken)
            ?? throw new StockMovementValidationException("No stock available at this warehouse to issue.");
        var now = DateTime.UtcNow;
        item.ApplyIssue(request.Quantity, now);
        await SyncProductStockAsync(request.ProductId, -request.Quantity, cancellationToken);

        var movement = new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: request.MovementType,
            quantity: request.Quantity,
            unitCost: item.AvgCost,
            onHandAfter: item.OnHand,
            avgCostAfter: item.AvgCost,
            occurredAtUtc: now,
            sourceDocumentType: request.SourceDocumentType,
            sourceDocumentId: request.SourceDocumentId,
            sourceLineId: request.SourceLineId,
            sourceReference: request.SourceReference,
            lotId: request.LotId,
            serialNumber: request.SerialNumber,
            reasonCodeId: request.ReasonCodeId,
            postedByUserId: request.PostedByUserId,
            notes: request.Notes);
        await _movements.AddAsync(movement, cancellationToken);
        return movement;
    }

    // Keeps the product-level rollup (Product.StockQuantity) in lockstep with the
    // warehouse-level ledger (StockItem.OnHand) for direct stock movements. The
    // order-confirm availability guard reads Product.StockQuantity, so a receipt
    // that only raised StockItem.OnHand would otherwise be invisible (false
    // InsufficientStock) and an issue that only drained StockItem.OnHand would
    // leave a phantom sellable balance (over-sell). Mirrors ConsumeAsync.
    private async Task SyncProductStockAsync(Guid productId, decimal delta, CancellationToken cancellationToken)
    {
        if (delta == 0m) return;
        var product = await _products.GetByIdAsync(productId, cancellationToken);
        if (product is null || !product.IsStockTracked) return;
        product.AdjustStock(delta);
        _products.Update(product);
    }

    public async Task<StockMovement> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Delta == 0m)
        {
            throw new StockMovementValidationException("Adjustment delta cannot be zero.");
        }
        var item = await _stockItems.GetOrCreateAsync(request.ProductId, request.WarehouseId, request.LotId, cancellationToken);
        var now = DateTime.UtcNow;
        item.ApplyAdjustment(request.Delta, request.UnitCost, now);
        await SyncProductStockAsync(request.ProductId, request.Delta, cancellationToken);

        var movementType = request.Delta > 0
            ? request.PositiveMovementType ?? StockMovementType.AdjustmentPositive
            : request.NegativeMovementType ?? StockMovementType.AdjustmentNegative;

        var movement = new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: movementType,
            quantity: Math.Abs(request.Delta),
            unitCost: request.UnitCost ?? item.AvgCost,
            onHandAfter: item.OnHand,
            avgCostAfter: item.AvgCost,
            occurredAtUtc: now,
            sourceDocumentType: request.SourceDocumentType,
            sourceDocumentId: request.SourceDocumentId,
            lotId: request.LotId,
            reasonCodeId: request.ReasonCodeId,
            postedByUserId: request.PostedByUserId,
            notes: request.Notes);
        await _movements.AddAsync(movement, cancellationToken);
        return movement;
    }

    public async Task<StockTransferResult> ApplyTransferAsync(
        Guid productId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        decimal quantity,
        string? reference = null,
        CancellationToken cancellationToken = default)
    {
        if (fromWarehouseId == toWarehouseId)
        {
            throw new StockMovementValidationException("Transfer source and destination warehouses must differ.");
        }
        if (quantity <= 0m)
        {
            throw new StockMovementValidationException("Transfer quantity must be positive.");
        }

        var source = await _stockItems.GetAsync(productId, fromWarehouseId, null, cancellationToken)
            ?? throw new StockMovementValidationException("No stock available at the source warehouse to transfer.");
        var unitCost = source.AvgCost;
        var sourceDocumentId = Guid.NewGuid();

        // Leg 1: issue at the source as TransferOut. Reuses the no-oversell guard, so
        // insufficient source stock throws before any movement is written (no partial
        // transfer). SyncProductStockAsync mirrors -quantity onto Product.StockQuantity.
        var transferOut = await ApplyIssueAsync(new StockIssueRequest(
            ProductId: productId,
            WarehouseId: fromWarehouseId,
            Quantity: quantity,
            SourceDocumentType: StockSourceDocumentType.Transfer,
            SourceDocumentId: sourceDocumentId,
            SourceLineId: null,
            SourceReference: reference,
            LotId: null,
            SerialNumber: null,
            ReasonCodeId: null,
            Notes: "Inter-warehouse transfer (out)",
            MovementType: StockMovementType.TransferOut), cancellationToken);

        // Leg 2: receive at the destination as TransferIn, valued at the SOURCE unit
        // cost so total inventory value is unchanged. The destination AvgCost recomputes
        // as the weighted average of its existing stock + the incoming at source cost.
        // SyncProductStockAsync mirrors +quantity onto Product.StockQuantity, netting the
        // leg-1 -quantity back to zero on that global scalar.
        var transferIn = await ApplyReceiptAsync(new StockReceiptRequest(
            ProductId: productId,
            WarehouseId: toWarehouseId,
            Quantity: quantity,
            UnitCost: unitCost,
            SourceDocumentType: StockSourceDocumentType.Transfer,
            SourceDocumentId: sourceDocumentId,
            SourceLineId: null,
            SourceReference: reference,
            LotId: null,
            SerialNumber: null,
            ReasonCodeId: null,
            Notes: "Inter-warehouse transfer (in)",
            MovementType: StockMovementType.TransferIn), cancellationToken);

        return new StockTransferResult(
            ProductId: productId,
            FromWarehouseId: fromWarehouseId,
            ToWarehouseId: toWarehouseId,
            Quantity: quantity,
            UnitCost: unitCost,
            FromOnHandAfter: transferOut.OnHandAfter,
            ToOnHandAfter: transferIn.OnHandAfter,
            SourceDocumentId: sourceDocumentId,
            TransferOut: transferOut,
            TransferIn: transferIn);
    }
}
