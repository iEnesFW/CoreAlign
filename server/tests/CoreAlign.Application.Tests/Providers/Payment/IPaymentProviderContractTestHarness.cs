using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Providers.Payment;

namespace CoreAlign.Application.Tests.Providers.Payment;

public sealed class IPaymentProviderContractTestHarness
{
    private const string WebhookSecret = "payment-test-secret";
    private readonly HashSet<string> _seenReplayKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChargeOutcomeQueueEntry> _idempotencyStore = new(StringComparer.Ordinal);

    public PaymentChargeOutcome? NextChargeOutcome { get; set; }
    public Payment3DSecureInitResult? NextInitResult { get; set; }
    public Payment3DSecureVerifyResult? NextVerifyResult { get; set; }
    public PaymentRefundResult? NextRefundResult { get; set; }
    public PaymentTransactionInfo? NextTransactionInfo { get; set; }
    public string? NextTokenizedCardToken { get; set; }

    public Exception? NextChargeException { get; set; }
    public Exception? NextInitException { get; set; }
    public Exception? NextVerifyException { get; set; }
    public Exception? NextRefundException { get; set; }
    public Exception? NextStatusException { get; set; }
    public Exception? NextTokenizeException { get; set; }

    public PaymentChargeRequest? LastChargeRequest { get; private set; }
    public Payment3DSecureRequest? LastInitRequest { get; private set; }
    public Payment3DSecureCallback? LastVerifyCallback { get; private set; }
    public string? LastRefundTransactionId { get; private set; }
    public decimal? LastRefundAmount { get; private set; }
    public string? LastStatusQueryTxId { get; private set; }
    public string? LastTokenizeRawPan { get; private set; }

    public int ChargeAttempts { get; private set; }
    public int RefundAttempts { get; private set; }
    public int StatusAttempts { get; private set; }
    public int InitAttempts { get; private set; }
    public int VerifyAttempts { get; private set; }
    public int TokenizeAttempts { get; private set; }

    private readonly Queue<TransientFailure> _chargeFailureQueue = new();
    private readonly Queue<TransientFailure> _statusFailureQueue = new();
    private readonly HashSet<string> _refundedTransactions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTime> _settlementWindows = new(StringComparer.Ordinal);

    public void QueueChargeFailure(TransientFailure failure) => _chargeFailureQueue.Enqueue(failure);

    public void QueueStatusFailure(TransientFailure failure) => _statusFailureQueue.Enqueue(failure);

    public void MarkTransactionRefunded(string transactionId) => _refundedTransactions.Add(transactionId);

    public void MarkSettlementWindowExpired(string transactionId, DateTime expiredOnUtc) =>
        _settlementWindows[transactionId] = expiredOnUtc;

    public Task<PaymentChargeOutcome> RecordChargeAsync(PaymentChargeRequest request, CancellationToken ct)
    {
        ChargeAttempts++;
        LastChargeRequest = request;
        ct.ThrowIfCancellationRequested();

        if (request.Metadata is not null && request.Metadata.TryGetValue("idempotency-key", out var idem) && !string.IsNullOrWhiteSpace(idem))
        {
            if (_idempotencyStore.TryGetValue(idem, out var prior))
            {
                return Task.FromResult(prior.Outcome);
            }
        }

        if (_chargeFailureQueue.Count > 0)
        {
            var failure = _chargeFailureQueue.Peek();
            if (failure.RemainingAttempts > 0)
            {
                _chargeFailureQueue.Dequeue();
                if (failure.RemainingAttempts - 1 > 0)
                {
                    _chargeFailureQueue.Enqueue(failure with { RemainingAttempts = failure.RemainingAttempts - 1 });
                }
                throw new HttpRequestException("Simulated transient 5xx");
            }
        }

        if (NextChargeException is not null)
        {
            throw NextChargeException;
        }

        if (NextChargeOutcome is null)
        {
            throw new InvalidOperationException("Harness has no ChargeOutcome configured.");
        }

        var outcome = NextChargeOutcome;
        if (request.Metadata is not null && request.Metadata.TryGetValue("idempotency-key", out var idem2) && !string.IsNullOrWhiteSpace(idem2))
        {
            _idempotencyStore[idem2] = new ChargeOutcomeQueueEntry(outcome);
        }
        return Task.FromResult(outcome);
    }

    public Task<Payment3DSecureInitResult> RecordInitiateAsync(Payment3DSecureRequest request, CancellationToken ct)
    {
        InitAttempts++;
        LastInitRequest = request;
        ct.ThrowIfCancellationRequested();
        if (NextInitException is not null) throw NextInitException;
        if (NextInitResult is null)
        {
            throw new InvalidOperationException("Harness has no InitResult configured.");
        }
        return Task.FromResult(NextInitResult);
    }

    public Task<Payment3DSecureVerifyResult> RecordVerifyAsync(Payment3DSecureCallback callback, CancellationToken ct)
    {
        VerifyAttempts++;
        LastVerifyCallback = callback;
        ct.ThrowIfCancellationRequested();
        if (NextVerifyException is not null) throw NextVerifyException;
        if (NextVerifyResult is null)
        {
            throw new InvalidOperationException("Harness has no VerifyResult configured.");
        }
        return Task.FromResult(NextVerifyResult);
    }

    public Task<PaymentRefundResult> RecordRefundAsync(string transactionId, decimal? amount, CancellationToken ct)
    {
        RefundAttempts++;
        LastRefundTransactionId = transactionId;
        LastRefundAmount = amount;
        ct.ThrowIfCancellationRequested();
        if (NextRefundException is not null) throw NextRefundException;

        if (_refundedTransactions.Contains(transactionId))
        {
            return Task.FromResult(new PaymentRefundResult(false, "harness", transactionId, null, null, "already_refunded", "Transaction already refunded."));
        }

        if (_settlementWindows.TryGetValue(transactionId, out var expiredOn) && expiredOn < DateTime.UtcNow)
        {
            return Task.FromResult(new PaymentRefundResult(false, "harness", transactionId, null, null, "refund_window_expired", "Refund window has closed."));
        }

        if (NextRefundResult is null)
        {
            throw new InvalidOperationException("Harness has no RefundResult configured.");
        }
        return Task.FromResult(NextRefundResult);
    }

    public Task<PaymentTransactionInfo> RecordGetTransactionAsync(string transactionId, CancellationToken ct)
    {
        StatusAttempts++;
        LastStatusQueryTxId = transactionId;
        ct.ThrowIfCancellationRequested();

        if (_statusFailureQueue.Count > 0)
        {
            var failure = _statusFailureQueue.Peek();
            if (failure.RemainingAttempts > 0)
            {
                _statusFailureQueue.Dequeue();
                if (failure.RemainingAttempts - 1 > 0)
                {
                    _statusFailureQueue.Enqueue(failure with { RemainingAttempts = failure.RemainingAttempts - 1 });
                }
                throw new HttpRequestException("Simulated transient 5xx");
            }
        }

        if (NextStatusException is not null) throw NextStatusException;
        if (NextTransactionInfo is null)
        {
            throw new PaymentTransactionNotFoundException(transactionId);
        }
        return Task.FromResult(NextTransactionInfo);
    }

    public Task<string> RecordTokenizeAsync(string rawPan, CancellationToken ct)
    {
        TokenizeAttempts++;
        LastTokenizeRawPan = rawPan;
        ct.ThrowIfCancellationRequested();
        if (NextTokenizeException is not null) throw NextTokenizeException;
        if (NextTokenizedCardToken is null)
        {
            throw new InvalidOperationException("Harness has no tokenized card token configured.");
        }
        return Task.FromResult(NextTokenizedCardToken);
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

    private sealed record ChargeOutcomeQueueEntry(PaymentChargeOutcome Outcome);
}
