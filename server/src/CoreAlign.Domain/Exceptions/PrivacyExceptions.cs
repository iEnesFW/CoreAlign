namespace CoreAlign.Domain.Exceptions;

public class PrivacyConfirmationMismatchException : DomainException
{
    public PrivacyConfirmationMismatchException()
        : base("Privacy.ConfirmationUsernameMismatch") { }
}

public class PrivacyUserNotFoundException : DomainException
{
    public PrivacyUserNotFoundException()
        : base("Privacy.UserNotFound") { }
}

public class PrivacyCustomerNotFoundException : DomainException
{
    public PrivacyCustomerNotFoundException()
        : base("Privacy.CustomerNotFound") { }
}

public class CustomerIsAnonymizedException : DomainException
{
    public CustomerIsAnonymizedException()
        : base("Privacy.CustomerIsAnonymized") { }
}

public class KvkkEraseAlreadyProcessedException : DomainException
{
    public KvkkEraseAlreadyProcessedException()
        : base("Privacy.KvkkEraseAlreadyProcessed") { }
}

public class DataSubjectRequestNotFoundException : DomainException
{
    public DataSubjectRequestNotFoundException()
        : base("Privacy.DataSubjectRequestNotFound") { }
}

public class DataSubjectRequestInvalidStateException : DomainException
{
    public DataSubjectRequestInvalidStateException(string reasonKey)
        : base(reasonKey) { }
}

public class RetentionPolicyNotFoundException : DomainException
{
    public RetentionPolicyNotFoundException()
        : base("Privacy.RetentionPolicyNotFound") { }
}
