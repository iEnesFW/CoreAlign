namespace CoreAlign.Domain.Exceptions;

public class DomainConcurrencyException : ConflictException
{
    public long CurrentVersion { get; }
    public long AttemptedVersion { get; }
    public IReadOnlyCollection<string> ConflictingFields { get; }

    public DomainConcurrencyException(long current, long attempted, IReadOnlyCollection<string>? conflictingFields = null)
        : base("Domain.ConcurrencyConflict")
    {
        CurrentVersion = current;
        AttemptedVersion = attempted;
        ConflictingFields = conflictingFields ?? Array.Empty<string>();
    }
}
