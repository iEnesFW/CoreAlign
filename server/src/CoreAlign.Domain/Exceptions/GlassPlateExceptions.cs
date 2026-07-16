namespace CoreAlign.Domain.Exceptions;

public class GlassPlateNotFoundException : NotFoundException
{
    public GlassPlateNotFoundException() : base("Glass plate not found.") { }
    public GlassPlateNotFoundException(Guid id) : base($"Glass plate {id} not found.") { }
}

public class StorageLocationNotFoundException : NotFoundException
{
    public StorageLocationNotFoundException() : base("Storage location not found.") { }
    public StorageLocationNotFoundException(Guid id) : base($"Storage location {id} not found.") { }
}

public class InvalidGlassPlateTransitionException : ConflictException
{
    public InvalidGlassPlateTransitionException(string from, string to)
        : base($"A glass plate cannot move from '{from}' to '{to}'.") { }
}

public class GlassPlateAreaExceededException : ConflictException
{
    public GlassPlateAreaExceededException(decimal requested, decimal remaining)
        : base($"Requested cut area {requested} mm² exceeds the plate's remaining {remaining} mm².") { }
}

public class GlassPlateNotTrackedException : ConflictException
{
    public GlassPlateNotTrackedException(Guid productId)
        : base($"Product {productId} is not plate-tracked; plate operations are not allowed.") { }
}

public class StorageLocationCodeConflictException : ConflictException
{
    public StorageLocationCodeConflictException(string code)
        : base($"A storage location with code '{code}' already exists in this warehouse.") { }
}

public class GlassPlateNumberConflictException : ConflictException
{
    public GlassPlateNumberConflictException(string plateNumber)
        : base($"A glass plate with number '{plateNumber}' already exists.") { }
}
