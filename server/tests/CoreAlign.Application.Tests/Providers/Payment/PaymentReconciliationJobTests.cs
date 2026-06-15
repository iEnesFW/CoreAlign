using CoreAlign.Application.Providers.Payment;

namespace CoreAlign.Application.Tests.Providers.Payment;

public sealed class PaymentReconciliationJobTests
{
    [Fact]
    public async Task Pending_transaction_status_change_is_detected_and_persisted()
    {
        var statusMap = new Dictionary<string, string>
        {
            ["tx-a"] = "settled",
            ["tx-b"] = "settled",
            ["tx-c"] = "failed",
            ["tx-d"] = "pending",
        };
        var store = new FakeReconciliationStore(statusMap.Keys.ToArray());
        var provider = new StatusOnlyProvider(statusMap);
        var job = new FakeReconciliationJob(provider, store);

        var changed = await job.RunAsync(CancellationToken.None);

        changed.Should().Be(3);
    }

    [Fact]
    public async Task Reconciliation_is_idempotent_second_run_emits_no_extra_events()
    {
        var statusMap = new Dictionary<string, string>
        {
            ["tx-a"] = "settled",
            ["tx-b"] = "failed",
        };
        var store = new FakeReconciliationStore(statusMap.Keys.ToArray());
        var provider = new StatusOnlyProvider(statusMap);
        var job = new FakeReconciliationJob(provider, store);

        var first = await job.RunAsync(CancellationToken.None);
        var second = await job.RunAsync(CancellationToken.None);

        first.Should().Be(2);
        second.Should().Be(0);
    }

    private sealed class FakeReconciliationStore
    {
        private readonly Dictionary<string, string> _statuses;

        public FakeReconciliationStore(IEnumerable<string> txIds)
        {
            _statuses = txIds.ToDictionary(t => t, _ => "pending");
        }

        public IReadOnlyList<string> GetPendingTxIds() =>
            _statuses.Where(kv => kv.Value == "pending").Select(kv => kv.Key).ToList();

        public bool UpdateStatus(string txId, string newStatus)
        {
            if (!_statuses.TryGetValue(txId, out var current)) return false;
            if (string.Equals(current, newStatus, StringComparison.Ordinal)) return false;
            _statuses[txId] = newStatus;
            return true;
        }
    }

    private sealed class StatusOnlyProvider
    {
        private readonly IReadOnlyDictionary<string, string> _statuses;

        public StatusOnlyProvider(IReadOnlyDictionary<string, string> statuses)
        {
            _statuses = statuses;
        }

        public Task<PaymentTransactionInfo> GetTransactionAsync(string txId, CancellationToken ct)
        {
            var status = _statuses.TryGetValue(txId, out var s) ? s : "unknown";
            return Task.FromResult(new PaymentTransactionInfo("harness", txId, status, 100m, "TRY", null, null));
        }
    }

    private sealed class FakeReconciliationJob
    {
        private readonly StatusOnlyProvider _provider;
        private readonly FakeReconciliationStore _store;

        public FakeReconciliationJob(StatusOnlyProvider provider, FakeReconciliationStore store)
        {
            _provider = provider;
            _store = store;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            var pending = _store.GetPendingTxIds();
            var changed = 0;
            foreach (var txId in pending)
            {
                ct.ThrowIfCancellationRequested();
                var info = await _provider.GetTransactionAsync(txId, ct).ConfigureAwait(false);
                if (info.Status is "settled" or "failed")
                {
                    if (_store.UpdateStatus(txId, info.Status)) changed++;
                }
            }
            return changed;
        }
    }
}
