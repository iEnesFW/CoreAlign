using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

public sealed class FefoLotSelector : IFefoLotSelector
{
    private readonly IStockItemRepository _stockItems;
    private readonly ILotRepository _lots;

    public FefoLotSelector(IStockItemRepository stockItems, ILotRepository lots)
    {
        _stockItems = stockItems;
        _lots = lots;
    }

    public async Task<IReadOnlyList<LotAllocationLine>> SelectAsync(
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0m)
        {
            return Array.Empty<LotAllocationLine>();
        }

        var stockItems = await _stockItems.GetByProductAsync(productId, cancellationToken);
        var lots = (await _lots.GetByProductAsync(productId, cancellationToken)).ToDictionary(l => l.Id);

        var candidates = stockItems
            .Where(si => si.WarehouseId == warehouseId && si.LotId.HasValue && si.AvailableToPromise > 0m)
            .Select(si => (Item: si, Lot: lots.GetValueOrDefault(si.LotId!.Value)))
            .Where(x => x.Lot is not null && !x.Lot.IsBlocked && !x.Lot.IsExpired(asOfUtc))
            .OrderBy(x => x.Lot!.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(x => x.Lot!.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Lot!.LotNumber)
            .ToList();

        var plan = new List<LotAllocationLine>();
        var remaining = quantity;
        foreach (var candidate in candidates)
        {
            if (remaining <= 0m)
            {
                break;
            }
            var take = Math.Min(remaining, candidate.Item.AvailableToPromise);
            if (take <= 0m)
            {
                continue;
            }
            plan.Add(new LotAllocationLine(candidate.Item.LotId!.Value, take));
            remaining -= take;
        }

        if (remaining > 0m)
        {
            throw new InsufficientStockException(productId.ToString(), quantity - remaining, quantity);
        }

        return plan;
    }
}
