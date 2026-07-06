namespace CoreAlign.Domain.Exceptions;

public class SerialUnitNotFoundException : NotFoundException
{
    public SerialUnitNotFoundException(Guid productId, string serialNumber)
        : base($"Serial '{serialNumber}' was not found for product {productId}.") { }
}

public class DuplicateSerialUnitException : ConflictException
{
    public DuplicateSerialUnitException(Guid productId, IEnumerable<string> serialNumbers)
        : base($"Serial(s) already registered for product {productId}: {string.Join(", ", serialNumbers)}.") { }
}

public class ProductNotSerialTrackedException : DomainException
{
    public ProductNotSerialTrackedException(Guid productId)
        : base($"Product {productId} is not serial-tracked; enable serial tracking before registering serials.") { }
}
