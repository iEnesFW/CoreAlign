namespace CoreAlign.Domain.Common;

public interface IHasConcurrencyToken
{
    long ConcurrencyToken { get; }
    void BumpConcurrencyToken();
}
