using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Infrastructure.Providers.EFatura;

public sealed class MockEFaturaProvider : IEFaturaProvider
{
    private static readonly DateTime SubmittedAtUtcFixed = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public string Name => "mock";

    public string DisplayName => "Mock EFatura Provider";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.Invoice
            | ProviderCapability.Despatch
            | ProviderCapability.Cancel
            | ProviderCapability.Archive,
        new Dictionary<string, string> { ["env"] = "dev" });

    public EFaturaProviderCapabilities SupportedCapabilities =>
        EFaturaProviderCapabilities.CanIssue
        | EFaturaProviderCapabilities.CanCancel
        | EFaturaProviderCapabilities.CanQueryStatus;

    public object? UnprotectCredentials(IProviderCredentialProtector protector, Guid tenantId, string? encryptedJson) => null;

    public Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new EFaturaIssueResult(
            Uuid: BuildEttn(),
            Status: "Accepted",
            GibStatus: "1000",
            SentAtUtc: SubmittedAtUtcFixed));
    }

    public Task<EFaturaCancelResult> CancelAsync(EFaturaCancelInvoiceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new EFaturaCancelResult(
            Ettn: request.Uuid,
            Cancelled: true,
            Reason: request.Reason));
    }

    public Task<EFaturaProviderStatus> GetStatusAsync(EFaturaGetStatusRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new EFaturaProviderStatus(
            Uuid: request.Uuid,
            CurrentStatus: "Accepted",
            GibResponseCode: "1000",
            DeliveredAtUtc: SubmittedAtUtcFixed));
    }

    private static string BuildEttn() =>
        "MOCK-" + Guid.NewGuid().ToString("N").Substring(0, 16);
}
