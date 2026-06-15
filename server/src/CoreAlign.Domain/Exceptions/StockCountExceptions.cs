namespace CoreAlign.Domain.Exceptions;

public class StockCountNotFoundException : NotFoundException
{
    public StockCountNotFoundException() : base("Stock count not found.") { }
}

public class StockCountLineNotFoundException : NotFoundException
{
    public StockCountLineNotFoundException() : base("Stock count line not found.") { }
}

public class InvalidStockCountStateException : ConflictException
{
    public InvalidStockCountStateException(string current, string attempted)
        : base($"Stock count is '{current}', cannot perform '{attempted}'.") { }
}

public class DuplicateStockCountNumberException : ConflictException
{
    public DuplicateStockCountNumberException() : base("A stock count with this number already exists.") { }
}

public class VendorPaymentApplicationNotFoundException : NotFoundException
{
    public VendorPaymentApplicationNotFoundException() : base("Vendor payment application not found.") { }
}

public class VendorPaymentImmutableException : ConflictException
{
    public VendorPaymentImmutableException()
        : base("Vendor payment cannot be modified once it has been applied or voided.") { }
}

public class VendorPaymentAlreadyVoidedException : ConflictException
{
    public VendorPaymentAlreadyVoidedException()
        : base("Vendor payment is already voided.") { }
}

public class VendorPaymentOverApplicationException : ConflictException
{
    public VendorPaymentOverApplicationException()
        : base("Applied amount exceeds the unapplied balance of the vendor payment.") { }
}

public class VendorPaymentBillMismatchException : ConflictException
{
    public VendorPaymentBillMismatchException()
        : base("Vendor payment and bill must belong to the same vendor and currency, and the bill must be open.") { }
}
