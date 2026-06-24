namespace CoreAlign.Domain.Exceptions;

public class ErrorLogNotFoundException : NotFoundException
{
    public ErrorLogNotFoundException(Guid id)
        : base($"Error log {id} was not found.") { }
}
