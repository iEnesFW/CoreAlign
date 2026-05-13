namespace CoreAlign.Domain.Exceptions;

public class InsufficientAvailableStockException : Exception
{
    public InsufficientAvailableStockException(string sku, string warehouseCode, decimal requested, decimal available)
        : base($"Insufficient available stock for {sku} at {warehouseCode}: requested {requested}, available {available}.")
    {
        Sku = sku;
        WarehouseCode = warehouseCode;
        Requested = requested;
        Available = available;
    }

    public string Sku { get; }
    public string WarehouseCode { get; }
    public decimal Requested { get; }
    public decimal Available { get; }
}

public class StockMovementValidationException : Exception
{
    public StockMovementValidationException(string message) : base(message) { }
}

public class AllocationNotFoundException : Exception
{
    public AllocationNotFoundException(Guid allocationId) : base($"Allocation {allocationId} was not found.") { }
}
