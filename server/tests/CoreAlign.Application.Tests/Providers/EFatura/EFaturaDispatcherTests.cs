using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class EFaturaDispatcherTests
{
    [Fact]
    public async Task Dispatcher_failover_primary_5xx_secondary_succeeds()
    {
        var primaryHarness = new IEFaturaContractTestHarness();
        var secondaryHarness = new IEFaturaContractTestHarness();

        primaryHarness.NextIssueException = new HttpRequestException("primary down");
        secondaryHarness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);

        var dispatcher = new FakeEFaturaDispatcher(new IEFaturaProvider[]
        {
            new HarnessBackedEFaturaProvider("nilvera", primaryHarness, maxRetriesOnTransient: 0),
            new HarnessBackedEFaturaProvider("foriba", secondaryHarness, maxRetriesOnTransient: 0),
        });

        var doc = BuildDocument();
        var result = await dispatcher.IssueAsync(new EFaturaIssueRequest(doc, "x"), CancellationToken.None);

        result.Status.Should().Be("Accepted");
        primaryHarness.IssueAttempts.Should().Be(1);
        secondaryHarness.IssueAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Dispatcher_all_providers_fail_throws_aggregate()
    {
        var p1 = new IEFaturaContractTestHarness { NextIssueException = new HttpRequestException("p1") };
        var p2 = new IEFaturaContractTestHarness { NextIssueException = new HttpRequestException("p2") };

        var dispatcher = new FakeEFaturaDispatcher(new IEFaturaProvider[]
        {
            new HarnessBackedEFaturaProvider("nilvera", p1, maxRetriesOnTransient: 0),
            new HarnessBackedEFaturaProvider("foriba", p2, maxRetriesOnTransient: 0),
        });

        var act = async () => await dispatcher.IssueAsync(new EFaturaIssueRequest(BuildDocument(), "x"), CancellationToken.None);

        await act.Should().ThrowAsync<AggregateException>()
            .WithMessage("*All providers failed*");
    }

    [Fact]
    public async Task Dispatcher_single_provider_no_failover_attempt()
    {
        var harness = new IEFaturaContractTestHarness
        {
            NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow),
        };
        var dispatcher = new FakeEFaturaDispatcher(new IEFaturaProvider[]
        {
            new HarnessBackedEFaturaProvider("nilvera", harness, maxRetriesOnTransient: 0),
        });

        var result = await dispatcher.IssueAsync(new EFaturaIssueRequest(BuildDocument(), "x"), CancellationToken.None);

        result.Status.Should().Be("Accepted");
        harness.IssueAttempts.Should().Be(1);
    }

    private static EFaturaDocument BuildDocument() =>
        new(
            EFaturaDocumentType.Invoice,
            "INV-DISP",
            new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            "1234567890",
            "Buyer Co",
            new[] { new EFaturaLine(1m, "Item", 100m, 20m) },
            "TRY",
            120m);

    private sealed class FakeEFaturaDispatcher
    {
        private readonly IReadOnlyList<IEFaturaProvider> _providers;

        public FakeEFaturaDispatcher(IEnumerable<IEFaturaProvider> providers)
        {
            _providers = providers.ToList();
        }

        public async Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct)
        {
            var failures = new List<Exception>();
            foreach (var provider in _providers)
            {
                try
                {
                    return await provider.IssueAsync(request, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            throw new AggregateException("All providers failed", failures);
        }
    }
}
