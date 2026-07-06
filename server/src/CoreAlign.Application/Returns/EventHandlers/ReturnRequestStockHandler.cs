using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Orders.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Returns.EventHandlers;

public class ReturnRequestReceivedStockHandler : INotificationHandler<ReturnRequestReceivedEvent>
{
    public const string ReasonNote = "RMA_RECEIVE";

    private readonly IProductRepository _productRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IGLPostingOutbox _glOutbox;
    private readonly Inventory.Services.IInventoryCostingService _costing;

    public ReturnRequestReceivedStockHandler(
        IProductRepository productRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        IStockTransactionRepository stockTransactionRepository,
        IGLPostingOutbox glOutbox,
        Inventory.Services.IInventoryCostingService costing)
    {
        _productRepository = productRepository;
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _glOutbox = glOutbox;
        _costing = costing;
    }

    public async Task Handle(ReturnRequestReceivedEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        var aggregated = notification.Lines
            .GroupBy(l => l.ProductId)
            .Select(g =>
            {
                var totalQty = g.Sum(x => x.QuantityReturned);
                var weightedCost = totalQty > 0m
                    ? g.Sum(x => x.QuantityReturned * x.UnitCostSnapshot) / totalQty
                    : 0m;
                return (ProductId: g.Key, Qty: totalQty, UnitCost: weightedCost);
            })
            .ToList();

        var cogsCost = 0m;
        foreach (var (productId, qty, capturedUnitCost) in aggregated)
        {
            if (!products.TryGetValue(productId, out var product) || !product.IsStockTracked)
            {
                continue;
            }
            product.AdjustStock(qty);
            _productRepository.Update(product);

            var stockItem = await _stockItemRepository.GetOrCreateAsync(
                product.Id, notification.WarehouseId, null, cancellationToken);
            var receiptUnitCost = capturedUnitCost > 0m ? capturedUnitCost : stockItem.AvgCost;
            stockItem.ApplyReceipt(qty, receiptUnitCost, notification.OccurredAtUtc);
            var movement = new StockMovement(
                productId: product.Id,
                warehouseId: notification.WarehouseId,
                type: StockMovementType.Receipt,
                quantity: qty,
                unitCost: receiptUnitCost,
                onHandAfter: stockItem.OnHand,
                avgCostAfter: stockItem.AvgCost,
                occurredAtUtc: notification.OccurredAtUtc,
                sourceDocumentType: StockSourceDocumentType.Return,
                sourceDocumentId: notification.ReturnRequestId,
                sourceReference: notification.ReturnNumber,
                notes: ReasonNote);
            await _stockMovementRepository.AddAsync(movement, cancellationToken);
            // Returned goods re-enter the FIFO stack as a new layer at their captured cost so the
            // next FIFO issue can consume them (no-op for non-Fifo products).
            await _costing.RecordReceiptLayerAsync(
                stockItem, product, qty, receiptUnitCost, notification.OccurredAtUtc, movement.Id, cancellationToken);
            cogsCost += movement.TotalCost;

            await _stockTransactionRepository.AddAsync(new StockTransaction(
                product.Id, StockTransactionType.Restock, qty, product.StockQuantity)
            {
                TenantId = notification.TenantId,
                OccurredAtUtc = notification.OccurredAtUtc,
                OrderId = notification.OrderId,
                Reference = notification.ReturnNumber,
                Notes = ReasonNote,
            }, cancellationToken);
        }

        // A received sales return puts goods back into stock → reverse the COGS:
        // DR Inventory(153) / CR CostOfGoodsSold(621). Keyed by
        // (CostOfGoodsSoldReversal, ReturnRequestId) for idempotency.
        if (cogsCost > 0m)
        {
            await _glOutbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.CostOfGoodsSoldReversal,
                notification.ReturnRequestId,
                notification.ReturnNumber,
                notification.OccurredAtUtc.Date,
                JournalEntryType.Mahsup,
                $"Satış maliyeti iadesi ({notification.ReturnNumber})",
                CogsGLLines.Build(cogsCost, reverse: true)), cancellationToken);
        }
    }
}
