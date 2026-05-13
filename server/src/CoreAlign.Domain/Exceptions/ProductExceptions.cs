namespace CoreAlign.Domain.Exceptions;

public class ProductNotFoundException : NotFoundException
{
    public ProductNotFoundException() : base("Product not found.") { }
}

public class DuplicateProductSkuException : ConflictException
{
    public DuplicateProductSkuException() : base("A product with this SKU already exists.") { }
}
