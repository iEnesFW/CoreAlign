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

public class PurchaseRequisitionNotFoundException : NotFoundException
{
    public PurchaseRequisitionNotFoundException() : base("Purchase requisition not found.") { }
}

public class DuplicatePurchaseRequisitionNumberException : ConflictException
{
    public DuplicatePurchaseRequisitionNumberException() : base("A purchase requisition with this number already exists.") { }
}
