namespace CoreAlign.Domain.Exceptions;

public class StockShortageBlocksConvertException : DomainException
{
    public IReadOnlyList<Guid> ShortageBomLineIds { get; }

    public StockShortageBlocksConvertException(IReadOnlyList<Guid> shortageBomLineIds)
        : base("Order.Convert.StockShortage")
    {
        ShortageBomLineIds = shortageBomLineIds;
    }
}
