namespace CoreAlign.Domain.Exceptions;

public class VendorNotFoundException : NotFoundException
{
    public VendorNotFoundException() : base("Vendor not found.") { }
    public VendorNotFoundException(Guid id) : base($"Vendor {id} not found.") { }
}

public class VendorCodeConflictException : ConflictException
{
    public VendorCodeConflictException(string code) : base($"A vendor with code '{code}' already exists.") { }
}

public class VendorTaxNumberConflictException : ConflictException
{
    public VendorTaxNumberConflictException(string taxNumber)
        : base($"Another vendor already uses tax number '{taxNumber}'.") { }
}

public class VendorNotApprovedException : ConflictException
{
    public VendorNotApprovedException()
        : base("Vendor is not approved; purchase documents cannot reference it until approval.") { }
}

public class VendorBlockedException : ConflictException
{
    public VendorBlockedException(string? reason)
        : base($"Vendor is blocked{(string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}")}.") { }
}

public class VendorChildNotFoundException : NotFoundException
{
    public VendorChildNotFoundException(string what) : base($"{what} not found on the specified vendor.") { }
}

public class InvalidVendorTypeException : DomainException
{
    public InvalidVendorTypeException(string raw) : base($"Invalid VendorType '{raw}'.") { }
}
