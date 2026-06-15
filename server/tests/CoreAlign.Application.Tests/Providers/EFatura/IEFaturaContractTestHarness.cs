using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class IEFaturaContractTestHarness
{
    private const string WebhookSecret = "test-secret";
    private readonly HashSet<string> _seenReplayKeys = new(StringComparer.Ordinal);

    public EFaturaIssueResult? NextIssueResult { get; set; }
    public EFaturaCancelResult? NextCancelResult { get; set; }
    public EFaturaProviderStatus? NextStatusResult { get; set; }
    public EFaturaCreditNoteResult? NextCreditNoteResult { get; set; }
    public IReadOnlyList<EFaturaInboxItem>? NextInbox { get; set; }

    public Exception? NextIssueException { get; set; }
    public Exception? NextCancelException { get; set; }
    public Exception? NextStatusException { get; set; }
    public Exception? NextCreditNoteException { get; set; }
    public Exception? NextInboxException { get; set; }

    public EFaturaIssueRequest? LastIssueRequest { get; private set; }
    public string? LastCancelUuid { get; private set; }
    public string? LastStatusUuid { get; private set; }
    public EFaturaCreditNoteRequest? LastCreditNoteRequest { get; private set; }
    public DateTime? LastInboxFrom { get; private set; }
    public DateTime? LastInboxTo { get; private set; }

    public int IssueAttempts { get; private set; }

    private readonly Queue<TransientFailure> _issueFailureQueue = new();

    public void QueueIssueFailure(TransientFailure failure) => _issueFailureQueue.Enqueue(failure);

    public Task<EFaturaIssueResult> RecordIssueAsync(EFaturaIssueRequest request, CancellationToken ct)
    {
        IssueAttempts++;
        LastIssueRequest = request;
        ct.ThrowIfCancellationRequested();

        if (_issueFailureQueue.Count > 0)
        {
            var failure = _issueFailureQueue.Peek();
            if (failure.RemainingAttempts > 0)
            {
                _issueFailureQueue.Dequeue();
                if (failure.RemainingAttempts - 1 > 0)
                {
                    _issueFailureQueue.Enqueue(failure with { RemainingAttempts = failure.RemainingAttempts - 1 });
                }
                throw new HttpRequestException("Simulated transient 5xx");
            }
        }

        if (NextIssueException is not null)
        {
            throw NextIssueException;
        }

        if (NextIssueResult is null)
        {
            throw new InvalidOperationException("Harness has no IssueResult configured.");
        }

        return Task.FromResult(NextIssueResult);
    }

    public Task<EFaturaCancelResult> RecordCancelAsync(string uuid, CancellationToken ct)
    {
        LastCancelUuid = uuid;
        ct.ThrowIfCancellationRequested();
        if (NextCancelException is not null) throw NextCancelException;
        return Task.FromResult(NextCancelResult ?? new EFaturaCancelResult(uuid, false, "no-config"));
    }

    public Task<EFaturaProviderStatus> RecordStatusAsync(string uuid, CancellationToken ct)
    {
        LastStatusUuid = uuid;
        ct.ThrowIfCancellationRequested();
        if (NextStatusException is not null) throw NextStatusException;
        return Task.FromResult(NextStatusResult ?? new EFaturaProviderStatus(uuid, "Unknown", null, null));
    }

    public Task<EFaturaCreditNoteResult> RecordCreditNoteAsync(EFaturaCreditNoteRequest request, CancellationToken ct)
    {
        LastCreditNoteRequest = request;
        ct.ThrowIfCancellationRequested();
        if (NextCreditNoteException is not null) throw NextCreditNoteException;
        if (NextCreditNoteResult is null)
        {
            throw new InvalidOperationException("Harness has no CreditNoteResult configured.");
        }
        return Task.FromResult(NextCreditNoteResult);
    }

    public Task<IReadOnlyList<EFaturaInboxItem>> RecordListReceivedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        LastInboxFrom = fromUtc;
        LastInboxTo = toUtc;
        ct.ThrowIfCancellationRequested();
        if (NextInboxException is not null) throw NextInboxException;
        return Task.FromResult(NextInbox ?? Array.Empty<EFaturaInboxItem>() as IReadOnlyList<EFaturaInboxItem>);
    }

    public string SignFor(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var key = Encoding.UTF8.GetBytes(WebhookSecret);
        var hmac = HMACSHA256.HashData(key, bytes);
        return Convert.ToHexString(hmac);
    }

    public bool VerifyWebhook(string payload, string signatureHex, bool enforceReplay = false)
    {
        var expected = SignFor(payload);
        var match = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signatureHex.ToUpperInvariant()));

        if (!match) return false;

        if (enforceReplay)
        {
            var key = payload + ":" + signatureHex;
            if (!_seenReplayKeys.Add(key))
            {
                return false;
            }
        }

        return true;
    }

    public void RegisterReplayGuard(string payload) => _seenReplayKeys.Clear();

    public sealed record TransientFailure(int RemainingAttempts);
}
