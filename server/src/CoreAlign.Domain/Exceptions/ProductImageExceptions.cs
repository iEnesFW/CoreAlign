namespace CoreAlign.Domain.Exceptions;

public class ProductImageNotFoundException : NotFoundException
{
    public ProductImageNotFoundException() : base("Product image not found.") { }
}

public class ProductImageLimitExceededException : ConflictException
{
    public ProductImageLimitExceededException(int max)
        : base($"Cannot exceed the maximum of {max} images per product.") { }
}

