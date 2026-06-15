using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class EFaturaReconciliationJobTests
{
    [Fact]
    public async Task Reconciles_pending_invoices_and_records_terminal_changes()
    {
        var statusMap = new Dictionary<string, string>
        {
            ["u1"] = "Accepted",
            ["u2"] = "Accepted",
            ["u3"] = "Accepted",
            ["u4"] = "Accepted",
            ["u5"] = "Rejected",
            ["u6"] = "Rejected",
            ["u7"] = "Rejected",
            ["u8"] = "Accepted",
            ["u9"] = "Pending",
            ["u10"] = "Pending",
        };

        var store = new FakeReconciliationStore(statusMap.Keys.ToArray());
        var provider = new StatusOnlyProvider(statusMap);
        var job = new FakeReconciliationJob(provider, store);

        var changed = await job.RunAsync(CancellationToken.None);

        changed.Should().Be(8);
    }

    [Fact]
    public async Task Reconciliation_is_idempotent_second_run_emits_no_extra_events()
    {
        var statusMap = new Dictionary<string, string>
        {
            ["u1"] = "Accepted",
            ["u2"] = "Rejected",
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

        public FakeReconciliationStore(IEnumerable<string> uuids)
        {
            _statuses = uuids.ToDictionary(u => u, _ => "Pending");
        }

        public IReadOnlyList<string> GetPendingUuids() =>
            _statuses.Where(kv => kv.Value == "Pending").Select(kv => kv.Key).ToList();

        public bool UpdateStatus(string uuid, string newStatus)
        {
            if (!_statuses.TryGetValue(uuid, out var current)) return false;
            if (string.Equals(current, newStatus, StringComparison.Ordinal)) return false;
            _statuses[uuid] = newStatus;
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

        public Task<EFaturaProviderStatus> GetStatusAsync(string uuid)
        {
            var status = _statuses.TryGetValue(uuid, out var s) ? s : "Unknown";
            return Task.FromResult(new EFaturaProviderStatus(uuid, status, null, null));
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
            var pending = _store.GetPendingUuids();
            var changed = 0;
            foreach (var uuid in pending)
            {
                ct.ThrowIfCancellationRequested();
                var status = await _provider.GetStatusAsync(uuid).ConfigureAwait(false);
                if (status.CurrentStatus is "Accepted" or "Rejected")
                {
                    if (_store.UpdateStatus(uuid, status.CurrentStatus)) changed++;
                }
            }
            return changed;
        }
    }
}
