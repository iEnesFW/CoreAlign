namespace CoreAlign.Application.Providers.EFatura;

public interface IEFaturaDispatcher
{
    Task<EFaturaDispatchResult> SubmitAsync(EFaturaDocument document, CancellationToken cancellationToken = default);

    Task<EFaturaStatus> GetStatusAsync(string ettn, string? providerNameOverride = null, CancellationToken cancellationToken = default);

    Task<EFaturaCancelResult> CancelAsync(string ettn, string reason, string? providerNameOverride = null, CancellationToken cancellationToken = default);

    Task<EFaturaTaxpayerStatus> CheckTaxpayerAsync(string taxNumber, string? providerNameOverride = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(DateTime fromUtc, DateTime toUtc, string? providerNameOverride = null, CancellationToken cancellationToken = default);
}

public sealed record EFaturaDispatchResult(
    EFaturaSubmitResult Result,
    string ProviderUsed,
    bool FailoverOccurred,
    IReadOnlyList<EFaturaAttemptInfo> AttemptHistory);

public sealed record EFaturaAttemptInfo(
    string ProviderName,
    bool Succeeded,
    string? ErrorMessage,
    DateTime AttemptedAtUtc,
    TimeSpan Duration);

public sealed class AllProvidersFailedException : Exception
{
    public IReadOnlyList<EFaturaAttemptInfo> Attempts { get; }

    public AllProvidersFailedException(string message, IReadOnlyList<EFaturaAttemptInfo> attempts)
        : base(message)
    {
        Attempts = attempts ?? Array.Empty<EFaturaAttemptInfo>();
    }

    public AllProvidersFailedException(string message, IReadOnlyList<EFaturaAttemptInfo> attempts, Exception innerException)
        : base(message, innerException)
    {
        Attempts = attempts ?? Array.Empty<EFaturaAttemptInfo>();
    }
}
