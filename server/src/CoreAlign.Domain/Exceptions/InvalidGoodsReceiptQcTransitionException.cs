namespace CoreAlign.Domain.Exceptions;

public class InvalidGoodsReceiptQcTransitionException : ConflictException
{
    public InvalidGoodsReceiptQcTransitionException(string from, string to)
        : base($"Goods receipt QC status cannot transition from '{from}' to '{to}'.") { }
}
