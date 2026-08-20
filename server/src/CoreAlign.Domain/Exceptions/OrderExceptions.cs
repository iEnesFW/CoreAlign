namespace CoreAlign.Domain.Exceptions;

public class OrderNotFoundException : NotFoundException
{
    public OrderNotFoundException() : base("Order not found.") { }
}

public class DuplicateOrderNumberException : ConflictException
{
    public DuplicateOrderNumberException() : base("An order with this number already exists.") { }
}

public class InvalidOrderLineException : DomainException
{
    public InvalidOrderLineException(string message) : base(message) { }
}

public class InvalidOrderStatusTransitionException : DomainException
{
    public InvalidOrderStatusTransitionException(string fromStatus, string toStatus)
        : base($"Cannot transition order from {fromStatus} to {toStatus}.")
    {
    }
}

public class OrderImmutableException : DomainException
{
    public OrderImmutableException(string status)
        : base($"Order header and lines can only be modified while in Draft status (current: {status}).")
    {
    }
}

public class OrderRevertBlockedException : ConflictException
{
    public OrderRevertBlockedException(string message) : base(message) { }
}

public class NoWarehouseConfiguredException : DomainException
{
    public NoWarehouseConfiguredException()
        : base(
            "Stok rezervasyonu için aktif bir depo bulunamadı. Lütfen Ayarlar → Tanımlar bölümünden en az bir depo ekleyin.")
    {
    }
}

public class InsufficientStockException : ConflictException
{
    public InsufficientStockException(string productName, decimal available, decimal requested)
        : base($"Insufficient stock for '{productName}'. Available: {available}, requested: {requested}.")
    {
    }
}

public class CreditLimitExceededException : ConflictException
{
    public CreditLimitExceededException(decimal limit, decimal projectedBalance)
        : base($"Customer credit limit ({limit}) would be exceeded (projected balance: {projectedBalance}).")
    {
    }
}

public class ShipmentNotFoundException : NotFoundException
{
    public ShipmentNotFoundException() : base("Shipment not found.") { }
}

public class EDespatchAlreadyIssuedException : ConflictException
{
    public EDespatchAlreadyIssuedException() : base("An e-despatch has already been issued for this shipment.") { }
}

public class InvalidShipmentStateException : DomainException
{
    public InvalidShipmentStateException(string message) : base(message) { }
}

public class ShipmentLineQuantityExceededException : DomainException
{
    public ShipmentLineQuantityExceededException(string sku, decimal remaining, decimal requested)
        : base($"Cannot ship {requested} of '{sku}'; only {remaining} remaining to ship.")
    {
    }
}

public class InvalidOrderApprovalStateException : DomainException
{
    public InvalidOrderApprovalStateException(string message) : base(message) { }
}

public class DealerCustomerNotAuthorizedException : ForbiddenException
{
    public DealerCustomerNotAuthorizedException()
        : base("This dealer is not authorized to act on behalf of the requested customer.")
    {
    }
}

public class OrderCancelBlockedException : ConflictException
{
    public OrderCancelBlockedException(string orderNumber, string shipmentNumber)
        : base($"Order {orderNumber} has shipment '{shipmentNumber}'. Cancel the shipment before cancelling the order.") { }
}

public class ShipmentOrderNotDispatchableException : ConflictException
{
    public ShipmentOrderNotDispatchableException(string shipmentNumber, string orderStatus)
        : base($"Shipment {shipmentNumber} cannot be dispatched while its order is {orderStatus}.") { }
}
