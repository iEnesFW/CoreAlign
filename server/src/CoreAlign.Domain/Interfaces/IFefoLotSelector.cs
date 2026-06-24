namespace CoreAlign.Domain.Interfaces;

public record LotAllocationLine(Guid LotId, decimal Quantity);

public interface IFefoLotSelector
{
    Task<IReadOnlyList<LotAllocationLine>> SelectAsync(
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
