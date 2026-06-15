namespace CoreAlign.Domain.Exceptions;

public class EnclosureConfigurationInvalidException : DomainException
{
    public IReadOnlyList<string> IssueKeys { get; }

    public EnclosureConfigurationInvalidException(IReadOnlyList<string> issueKeys)
        : base("GlassEnclosure.Configuration.Invalid")
    {
        IssueKeys = issueKeys;
    }
}
