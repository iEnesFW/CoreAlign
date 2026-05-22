using System.Linq;
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

    public AllocationService(
        IStockItemRepository stockItems,
        IStockMovementRepository movements,
        IStockAllocationRepository allocations,
        IWarehouseRepository warehouses,
        IProductRepository products)
    {
        _stockItems = stockItems;
        _movements = movements;
        _allocations = allocations;
        _warehouses = warehouses;
        _products = products;
    }

    public async Task<AllocationResult> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _stockItems.GetOrCreateAsync(request.ProductId, request.WarehouseId, request.LotId, cancellationToken);
        var now = DateTime.UtcNow;

        // Bridge: the first time a product is stocked in a warehouse, materialize
        // its recorded on-hand (Product.StockQuantity) as an opening balance so
        // existing stock becomes allocatable. Guarded to a fresh stock item with
        // no stock anywhere else, so it can never double-count across warehouses.
        if (item.OnHand == 0m && item.Reserved == 0m && item.LastMovementAtUtc is null)
        {
            var siblings = await _stockItems.GetByProductAsync(request.ProductId, cancellationToken);
            var hasStockElsewhere = siblings.Any(s => s.Id != item.Id && (s.OnHand != 0m || s.Reserved != 0m));
            if (!hasStockElsewhere)
            {
                var product = await _products.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is not null && product.StockQuantity > 0m)
                {
                    var openingCost = product.AverageCost > 0m ? product.AverageCost : product.StandardCost;
                    item.SeedOpeningBalance(product.StockQuantity, openingCost, now);
                    await _movements.AddAsync(new StockMovement(
                        productId: request.ProductId,
                        warehouseId: request.WarehouseId,
                        type: StockMovementType.OpeningBalance,
                        quantity: product.StockQuantity,
                        unitCost: openingCost,
                        onHandAfter: item.OnHand,
                        avgCostAfter: item.AvgCost,
                        occurredAtUtc: now,
                        sourceDocumentType: StockSourceDocumentType.OpeningBalance,
                        notes: "Açılış bakiyesi (ürün stoğundan otomatik)"
                    ), cancellationToken);
                }
            }
        }

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

    public async Task<decimal> ConsumeForOrderLineAsync(Guid orderId, Guid orderLineId, decimal quantity, Guid? postedByUserId, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0m) return 0m;

        var allocations = await _allocations.GetByOrderAsync(orderId, cancellationToken);
        var pending = allocations
            .Where(a => a.OrderLineId == orderLineId
                && (a.Status == AllocationStatus.Active || a.Status == AllocationStatus.PartiallyConsumed))
            .ToList();

        var remaining = quantity;
        var consumed = 0m;
        foreach (var allocation in pending)
        {
            if (remaining <= 0m) break;
            var take = Math.Min(remaining, allocation.Remaining);
            if (take <= 0m) continue;
            await ConsumeAsync(allocation.Id, take, postedByUserId, cancellationToken);
            remaining -= take;
            consumed += take;
        }

        return consumed;
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

        var movement = new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: StockMovementType.Receipt,
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

        var movement = new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: StockMovementType.Issue,
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

    public async Task<StockMovement> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Delta == 0m)
        {
            throw new StockMovementValidationException("Adjustment delta cannot be zero.");
        }
        var item = await _stockItems.GetOrCreateAsync(request.ProductId, request.WarehouseId, request.LotId, cancellationToken);
        var now = DateTime.UtcNow;
        item.ApplyAdjustment(request.Delta, request.UnitCost, now);

        var movement = new StockMovement(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            type: request.Delta > 0 ? StockMovementType.AdjustmentPositive : StockMovementType.AdjustmentNegative,
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
}
