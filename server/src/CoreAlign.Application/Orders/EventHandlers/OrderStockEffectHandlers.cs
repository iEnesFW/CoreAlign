using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.EventHandlers;

/// <summary>
/// Builds the COGS recognition journal that relieves inventory at issue cost.
/// On a sale issue: DR CostOfGoodsSold(621) / CR Inventory(153). On a return /
/// cancel that receives stock back the entry is reversed. Account codes resolve
/// through the tenant's <c>GLPostingMapping</c> (defaults 621 / 153) exactly like
/// the cycle-count variance posting — never hardcoded ids.
/// </summary>
internal static class CogsGLLines
{
    public static IReadOnlyList<GLPostingLine> Build(decimal cost, bool reverse)
    {
        var amount = Math.Round(Math.Abs(cost), 4);
        return reverse
            ? new[]
            {
                new GLPostingLine(GLPostingKey.Inventory, amount, 0m),
                new GLPostingLine(GLPostingKey.CostOfGoodsSold, 0m, amount),
            }
            : new[]
            {
                new GLPostingLine(GLPostingKey.CostOfGoodsSold, amount, 0m),
                new GLPostingLine(GLPostingKey.Inventory, 0m, amount),
            };
    }
}

public static class BomResolver
{
    public static Dictionary<Guid, decimal> ExpandToLeaves(
        IEnumerable<OrderLineSnapshot> lines,
        IReadOnlyDictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>> bomTree)
    {
        var leafTotals = new Dictionary<Guid, decimal>();
        foreach (var line in lines)
        {
            ExpandRecursive(line.ProductId, line.Quantity, bomTree, leafTotals, new HashSet<Guid>());
        }
        return leafTotals;
    }

    private static void ExpandRecursive(
        Guid productId,
        decimal multiplier,
        IReadOnlyDictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>> tree,
        Dictionary<Guid, decimal> leafTotals,
        HashSet<Guid> path)
    {
        if (!path.Add(productId))
        {
            throw new InvalidOperationException($"Cycle detected at product {productId}.");
        }

        if (!tree.TryGetValue(productId, out var children) || children.Count == 0)
        {
            if (leafTotals.TryGetValue(productId, out var existing))
            {
                leafTotals[productId] = existing + multiplier;
            }
            else
            {
                leafTotals[productId] = multiplier;
            }
        }
        else
        {
            foreach (var (componentId, quantity) in children)
            {
                ExpandRecursive(componentId, multiplier * quantity, tree, leafTotals, new HashSet<Guid>(path));
            }
        }
    }
}

public class OrderConfirmedStockHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductComponentRepository _componentRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IGLPostingOutbox _glOutbox;
    private readonly IStockOpeningBalanceBridge _openingBalanceBridge;

    public OrderConfirmedStockHandler(
        IProductRepository productRepository,
        IProductComponentRepository componentRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        IGLPostingOutbox glOutbox,
        IStockOpeningBalanceBridge openingBalanceBridge)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _glOutbox = glOutbox;
        _openingBalanceBridge = openingBalanceBridge;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var lineProductIds = notification.Lines.Select(l => l.ProductId).Distinct().ToList();
        var bomTree = await _componentRepository.GetTreeForProductsAsync(lineProductIds, cancellationToken);
        var leafTotals = BomResolver.ExpandToLeaves(notification.Lines, bomTree);

        var leafProductIds = leafTotals.Keys.ToList();
        var products = await _productRepository.GetByIdsAsync(leafProductIds, cancellationToken);

        var defaultWarehouse = await _warehouseRepository.GetDefaultAsync(cancellationToken);

        // Pre-flight availability gate, all-or-nothing: every leaf must pass before
        // any stock is issued. With a default warehouse configured the check is the
        // per-warehouse AvailableToPromise (OnHand - Reserved) of the warehouse the
        // issue actually draws from — a global StockQuantity that is sufficient only
        // in aggregate across warehouses must NOT pass (the issue would otherwise
        // backorder the default warehouse into negative on-hand). Without a
        // configured warehouse it falls back to the global rollup.
        var issueStockItems = new Dictionary<Guid, StockItem>();
        foreach (var (productId, required) in leafTotals)
        {
            var product = products[productId];
            if (!product.IsStockTracked)
            {
                continue;
            }

            if (defaultWarehouse is not null)
            {
                var stockItem = await _stockItemRepository.GetOrCreateAsync(product.Id, defaultWarehouse.Id, null, cancellationToken);
                // Materialize global on-hand into the warehouse ledger on first touch
                // (same bridge the allocation path uses) so a product stocked only at
                // the global scalar is not wrongly seen as 0-available here.
                await _openingBalanceBridge.EnsureMaterializedAsync(stockItem, cancellationToken);
                issueStockItems[productId] = stockItem;
                if (stockItem.AvailableToPromise < required)
                {
                    throw new InsufficientStockException(product.Name, stockItem.AvailableToPromise, required);
                }
            }
            else if (product.StockQuantity < required)
            {
                throw new InsufficientStockException(product.Name, product.StockQuantity, required);
            }
        }

        // Σ of the issued cost across the sale's lines; relieved from inventory to
        // COGS in a single balanced journal below (mirrors the receipt handler's
        // per-document GL entry).
        var cogsCost = 0m;
        foreach (var (productId, required) in leafTotals)
        {
            var product = products[productId];
            if (product.IsStockTracked)
            {
                product.AdjustStock(-required);
                _productRepository.Update(product);
            }

            var txn = new StockTransaction(product.Id, StockTransactionType.Sale, -required, product.StockQuantity)
            {
                TenantId = notification.TenantId,
                OccurredAtUtc = notification.OccurredAtUtc,
                OrderId = notification.OrderId,
                Reference = notification.OrderNumber,
                Notes = "Order confirmed (BOM-resolved)"
            };
            await _stockTransactionRepository.AddAsync(txn, cancellationToken);

            if (defaultWarehouse is not null && product.IsStockTracked)
            {
                var stockItem = issueStockItems[productId];
                var occurred = notification.OccurredAtUtc;
                stockItem.ApplyIssue(required, occurred, allowNegative: false);
                var movement = new StockMovement(
                    productId: product.Id,
                    warehouseId: defaultWarehouse.Id,
                    type: StockMovementType.Issue,
                    quantity: required,
                    unitCost: stockItem.AvgCost,
                    onHandAfter: stockItem.OnHand,
                    avgCostAfter: stockItem.AvgCost,
                    occurredAtUtc: occurred,
                    sourceDocumentType: StockSourceDocumentType.Order,
                    sourceDocumentId: notification.OrderId,
                    sourceReference: notification.OrderNumber,
                    notes: "Order confirmed (BOM-resolved)");
                await _stockMovementRepository.AddAsync(movement, cancellationToken);
                cogsCost += movement.TotalCost;
            }
        }

        // COGS recognition: relieve inventory at issue cost. Keyed by
        // (CostOfGoodsSold, OrderId) so a replay of the confirm event cannot
        // double-post — distinct from the SalesInvoice posting on the same order.
        if (cogsCost > 0m)
        {
            await _glOutbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.CostOfGoodsSold,
                notification.OrderId,
                notification.OrderNumber,
                notification.OccurredAtUtc.Date,
                JournalEntryType.Mahsup,
                $"Satış maliyeti ({notification.OrderNumber})",
                CogsGLLines.Build(cogsCost, reverse: false)), cancellationToken);
        }
    }
}

public class OrderCancelledStockHandler : INotificationHandler<OrderCancelledFromActiveEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductComponentRepository _componentRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IGLPostingOutbox _glOutbox;

    public OrderCancelledStockHandler(
        IProductRepository productRepository,
        IProductComponentRepository componentRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        IGLPostingOutbox glOutbox)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _glOutbox = glOutbox;
    }

    public async Task Handle(OrderCancelledFromActiveEvent notification, CancellationToken cancellationToken)
    {
        var lineProductIds = notification.Lines.Select(l => l.ProductId).Distinct().ToList();
        var bomTree = await _componentRepository.GetTreeForProductsAsync(lineProductIds, cancellationToken);
        var leafTotals = BomResolver.ExpandToLeaves(notification.Lines, bomTree);

        var leafProductIds = leafTotals.Keys.ToList();
        var products = await _productRepository.GetByIdsAsync(leafProductIds, cancellationToken);
        var defaultWarehouse = await _warehouseRepository.GetDefaultAsync(cancellationToken);

        var cogsCost = 0m;
        foreach (var (productId, restored) in leafTotals)
        {
            var product = products[productId];
            if (product.IsStockTracked)
            {
                product.AdjustStock(restored);
                _productRepository.Update(product);
            }

            var txn = new StockTransaction(product.Id, StockTransactionType.SaleCancelled, restored, product.StockQuantity)
            {
                TenantId = notification.TenantId,
                OccurredAtUtc = notification.OccurredAtUtc,
                OrderId = notification.OrderId,
                Reference = notification.OrderNumber,
                Notes = "Order cancelled (BOM-resolved)"
            };
            await _stockTransactionRepository.AddAsync(txn, cancellationToken);

            if (defaultWarehouse is not null && product.IsStockTracked)
            {
                var stockItem = await _stockItemRepository.GetOrCreateAsync(product.Id, defaultWarehouse.Id, null, cancellationToken);
                var occurred = notification.OccurredAtUtc;
                stockItem.ApplyReceipt(restored, stockItem.AvgCost, occurred);
                var movement = new StockMovement(
                    productId: product.Id,
                    warehouseId: defaultWarehouse.Id,
                    type: StockMovementType.Receipt,
                    quantity: restored,
                    unitCost: stockItem.AvgCost,
                    onHandAfter: stockItem.OnHand,
                    avgCostAfter: stockItem.AvgCost,
                    occurredAtUtc: occurred,
                    sourceDocumentType: StockSourceDocumentType.Order,
                    sourceDocumentId: notification.OrderId,
                    sourceReference: notification.OrderNumber,
                    notes: "Order cancelled (BOM-resolved)");
                await _stockMovementRepository.AddAsync(movement, cancellationToken);
                cogsCost += movement.TotalCost;
            }
        }

        // Cancelling an active sale receives stock back → reverse the COGS
        // recognition: DR Inventory(153) / CR CostOfGoodsSold(621). Keyed by
        // (CostOfGoodsSoldReversal, OrderId) so it never double-posts.
        if (cogsCost > 0m)
        {
            await _glOutbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.CostOfGoodsSoldReversal,
                notification.OrderId,
                notification.OrderNumber,
                notification.OccurredAtUtc.Date,
                JournalEntryType.Mahsup,
                $"Satış maliyeti iptali ({notification.OrderNumber})",
                CogsGLLines.Build(cogsCost, reverse: true)), cancellationToken);
        }
    }
}
