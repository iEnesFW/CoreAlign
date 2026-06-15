using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class HarnessBackedEFaturaProvider : IEFaturaProvider
{
    private readonly IEFaturaContractTestHarness _harness;
    private readonly int _maxRetriesOnTransient;

    public HarnessBackedEFaturaProvider(string name, IEFaturaContractTestHarness harness, int maxRetriesOnTransient = 3)
    {
        Name = name;
        _harness = harness;
        _maxRetriesOnTransient = maxRetriesOnTransient;
    }

    public string Name { get; }

    public string DisplayName => Name;

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.Invoice | ProviderCapability.Cancel | ProviderCapability.Refund | ProviderCapability.Webhook,
        new Dictionary<string, string> { ["mode"] = "contract-test" });

    public EFaturaProviderCapabilities SupportedCapabilities =>
        EFaturaProviderCapabilities.CanIssue
        | EFaturaProviderCapabilities.CanCancel
        | EFaturaProviderCapabilities.CanCreditNote
        | EFaturaProviderCapabilities.CanQueryStatus
        | EFaturaProviderCapabilities.CanListReceived
        | EFaturaProviderCapabilities.CanWebhook;

    public object? UnprotectCredentials(IProviderCredentialProtector protector, Guid tenantId, string? encryptedJson) => null;

    public async Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct)
    {
        for (var attempt = 0; attempt <= _maxRetriesOnTransient; attempt++)
        {
            try
            {
                return await _harness.RecordIssueAsync(request, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < _maxRetriesOnTransient)
            {
            }
        }

        throw new HttpRequestException("Retry budget exhausted.");
    }

    public Task<EFaturaCancelResult> CancelAsync(EFaturaCancelInvoiceRequest request, CancellationToken ct) =>
        _harness.RecordCancelAsync(request.Uuid, ct);

    public Task<EFaturaProviderStatus> GetStatusAsync(EFaturaGetStatusRequest request, CancellationToken ct) =>
        _harness.RecordStatusAsync(request.Uuid, ct);

    public Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(EFaturaListReceivedRequest request, CancellationToken ct) =>
        _harness.RecordListReceivedAsync(request.FromUtc, request.ToUtc, ct);

    public Task<EFaturaCreditNoteResult> CreditNoteAsync(EFaturaCreditNoteRequest request, CancellationToken ct) =>
        _harness.RecordCreditNoteAsync(request, ct);
}
