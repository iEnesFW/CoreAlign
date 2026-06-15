namespace CoreAlign.Domain.Exceptions;

public class MinOrderQuantityNotMetException : DomainException
{
    public Guid ProductId { get; }
    public int LineNumber { get; }
    public decimal RequestedQuantity { get; }
    public decimal MinQuantity { get; }

    public MinOrderQuantityNotMetException(Guid productId, int lineNumber, decimal requestedQuantity, decimal minQuantity)
        : base($"Line {lineNumber}: requested quantity {requestedQuantity} is below the minimum of {minQuantity} for the selected product.")
    {
        ProductId = productId;
        LineNumber = lineNumber;
        RequestedQuantity = requestedQuantity;
        MinQuantity = minQuantity;
    }
}
