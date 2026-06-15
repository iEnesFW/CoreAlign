namespace CoreAlign.Domain.Exceptions;

public class BomStaleBlocksConvertException : DomainException
{
    public string? StaleReason { get; }

    public BomStaleBlocksConvertException(string? staleReason)
        : base("Order.Convert.BomStale")
    {
        StaleReason = staleReason;
    }
}

public class BomStaleBlocksShareException : DomainException
{
    public string? StaleReason { get; }

    public BomStaleBlocksShareException(string? staleReason)
        : base("Share.Generate.BomStale")
    {
        StaleReason = staleReason;
    }
}
