namespace CoreAlign.Domain.Exceptions;

public class ProductComponentNotFoundException : NotFoundException
{
    public ProductComponentNotFoundException() : base("Product component not found.") { }
}

public class DuplicateProductComponentException : ConflictException
{
    public DuplicateProductComponentException() : base("This component is already part of the parent product.") { }
}

public class CircularProductComponentException : DomainException
{
    public CircularProductComponentException(string parentSku, string componentSku)
        : base($"Adding '{componentSku}' to '{parentSku}' would create a circular composition.")
    {
    }
}
