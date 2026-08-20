namespace CoreAlign.Domain.Exceptions;

public class PurchaseOrderNotFoundException : NotFoundException
{
    public PurchaseOrderNotFoundException() : base("Purchase order not found.") { }
}

public class DuplicatePurchaseOrderNumberException : ConflictException
{
    public DuplicatePurchaseOrderNumberException() : base("A purchase order with this number already exists.") { }
}

public class VendorNotFoundForPurchaseException : NotFoundException
{
    public VendorNotFoundForPurchaseException() : base("Vendor not found.") { }
}

public class VendorBillNotFoundException : NotFoundException
{
    public VendorBillNotFoundException() : base("Vendor bill not found.") { }
    public VendorBillNotFoundException(Guid id) : base($"Vendor bill {id} not found.") { }
}

public class DuplicateVendorBillNumberException : ConflictException
{
    public DuplicateVendorBillNumberException() : base("A bill with this number already exists for the vendor.") { }
}

public class PurchaseOrderLineNotFoundForBillException : NotFoundException
{
    public PurchaseOrderLineNotFoundForBillException()
        : base("The referenced purchase order line was not found on the bill's purchase order.") { }
}

public class GoodsReceiptNotFoundException : NotFoundException
{
    public GoodsReceiptNotFoundException() : base("Goods receipt not found.") { }
}

public class GoodsReceiptAlreadyBilledException : ConflictException
{
    public GoodsReceiptAlreadyBilledException()
        : base("This goods receipt cannot be reversed because some or all of its quantity has already been billed.") { }
}

public class PurchaseRequisitionNotFoundException : NotFoundException
{
    public PurchaseRequisitionNotFoundException() : base("Purchase requisition not found.") { }
}

public class DuplicatePurchaseRequisitionNumberException : ConflictException
{
    public DuplicatePurchaseRequisitionNumberException() : base("A purchase requisition with this number already exists.") { }
}

public class ReceiptReversalBelowBilledException : ConflictException
{
    public ReceiptReversalBelowBilledException(string productSku, decimal billed, decimal wouldLeave)
        : base($"Cannot un-receive '{productSku}': {billed} is already billed and the reversal would leave only {wouldLeave} received.") { }
}

public class VendorBillCancelBlockedByPaymentException : ConflictException
{
    public VendorBillCancelBlockedByPaymentException(string billNumber, decimal amountPaid)
        : base($"Bill '{billNumber}' cannot be cancelled while {amountPaid} is applied to it. Void or unapply the payment first.") { }
}
