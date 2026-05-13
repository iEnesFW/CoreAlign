using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.EventHandlers;

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

    public OrderConfirmedStockHandler(
        IProductRepository productRepository,
        IProductComponentRepository componentRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var lineProductIds = notification.Lines.Select(l => l.ProductId).Distinct().ToList();
        var bomTree = await _componentRepository.GetTreeForProductsAsync(lineProductIds, cancellationToken);
        var leafTotals = BomResolver.ExpandToLeaves(notification.Lines, bomTree);

        var leafProductIds = leafTotals.Keys.ToList();
        var products = await _productRepository.GetByIdsAsync(leafProductIds, cancellationToken);

        foreach (var (productId, required) in leafTotals)
        {
            var product = products[productId];
            if (product.IsStockTracked && product.StockQuantity < required)
            {
                throw new InsufficientStockException(product.Name, product.StockQuantity, required);
            }
        }

        var defaultWarehouse = await _warehouseRepository.GetDefaultAsync(cancellationToken);

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
                var stockItem = await _stockItemRepository.GetOrCreateAsync(product.Id, defaultWarehouse.Id, null, cancellationToken);
                var occurred = notification.OccurredAtUtc;
                stockItem.ApplyIssue(required, occurred, allowNegative: true);
                await _stockMovementRepository.AddAsync(new StockMovement(
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
                    notes: "Order confirmed (BOM-resolved)"), cancellationToken);
            }
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

    public OrderCancelledStockHandler(
        IProductRepository productRepository,
        IProductComponentRepository componentRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task Handle(OrderCancelledFromActiveEvent notification, CancellationToken cancellationToken)
    {
        var lineProductIds = notification.Lines.Select(l => l.ProductId).Distinct().ToList();
        var bomTree = await _componentRepository.GetTreeForProductsAsync(lineProductIds, cancellationToken);
        var leafTotals = BomResolver.ExpandToLeaves(notification.Lines, bomTree);

        var leafProductIds = leafTotals.Keys.ToList();
        var products = await _productRepository.GetByIdsAsync(leafProductIds, cancellationToken);
        var defaultWarehouse = await _warehouseRepository.GetDefaultAsync(cancellationToken);

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
                await _stockMovementRepository.AddAsync(new StockMovement(
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
                    notes: "Order cancelled (BOM-resolved)"), cancellationToken);
            }
        }
    }
}
