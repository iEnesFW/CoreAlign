namespace CoreAlign.Domain.Exceptions;

public class TagNotFoundException : NotFoundException
{
    public TagNotFoundException() : base("Tag not found.") { }
}

public class CustomerMergeSameIdException : ConflictException
{
    public CustomerMergeSameIdException()
        : base("Source and target customer must be different.") { }
}

public class CustomerMergeAlreadyArchivedException : ConflictException
{
    public CustomerMergeAlreadyArchivedException()
        : base("Source customer is already archived or anonymized.") { }
}

public class CustomerMergeConcurrencyException : ConflictException
{
    public CustomerMergeConcurrencyException()
        : base("Customer was modified concurrently. Refresh and retry.") { }
}

public class CustomerMergeIdempotencyConflictException : ConflictException
{
    public CustomerMergeIdempotencyConflictException()
        : base("Merge operation with this id already exists for a different source/target pair.") { }
}

public class CustomerStatementRangeTooLargeException : ConflictException
{
    public CustomerStatementRangeTooLargeException(int rowCount, int maxRows)
        : base($"Statement range contains {rowCount} entries which exceeds the maximum of {maxRows}. Narrow the date range.") { }
}
