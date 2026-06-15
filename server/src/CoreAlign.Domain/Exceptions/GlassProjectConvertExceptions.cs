namespace CoreAlign.Domain.Exceptions;

public class EmptyBomException : DomainException
{
    public EmptyBomException() : base("Order.Convert.EmptyBom") { }
}

public class BomLineProductLinkMissingException : DomainException
{
    public Guid BomLineId { get; }

    public BomLineProductLinkMissingException(Guid bomLineId)
        : base("Order.Convert.RequiresLinkedProducts")
    {
        BomLineId = bomLineId;
    }
}
