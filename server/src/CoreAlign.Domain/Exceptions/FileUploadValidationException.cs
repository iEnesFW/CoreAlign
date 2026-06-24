namespace CoreAlign.Domain.Exceptions;

public sealed class FileUploadValidationException : DomainException
{
    public FileUploadValidationException(string message) : base(message) { }
}
