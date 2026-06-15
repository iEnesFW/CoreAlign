namespace CoreAlign.Domain.Exceptions;

public class ReturnRequestNotFoundException : NotFoundException
{
    public ReturnRequestNotFoundException() : base("Return request not found.") { }
}

public class InvalidReturnRequestStateException : DomainException
{
    public InvalidReturnRequestStateException(string message) : base(message) { }
}

public class DuplicateReturnNumberException : ConflictException
{
    public DuplicateReturnNumberException() : base("A return request with this number already exists.") { }
}

public class CannotIssueCreditNoteException : DomainException
{
    public CannotIssueCreditNoteException(string message) : base(message) { }
}
