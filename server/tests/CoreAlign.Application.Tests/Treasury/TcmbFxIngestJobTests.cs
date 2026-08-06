using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Treasury;

public class TcmbFxIngestJobTests
{
    private readonly ITcmbFxClient _client = Substitute.For<ITcmbFxClient>();
    private readonly IExchangeRateRepository _repo = Substitute.For<IExchangeRateRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrencyCatalog _catalog = Substitute.For<ICurrencyCatalog>();

    public TcmbFxIngestJobTests() =>
        _catalog.ListAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Currency>());

    private CurrencyCatalogSync CatalogSync() => new(_catalog);

    [Fact]
    public async Task Inserts_new_rate_when_none_exists_for_currency_and_date()
    {
        var today = DateTime.SpecifyKind(new DateTime(2026, 6, 3), DateTimeKind.Utc);
        _client.FetchTodayAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new TcmbRate("USD", 32.18m, today) });
        _repo.GetAsync("USD", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns((ExchangeRate?)null);

        var sut = new TcmbFxIngestJob(_client, _repo, _uow, CatalogSync(), NullLogger<TcmbFxIngestJob>.Instance);
        var count = await sut.RunAsync();

        count.Should().Be(1);
        await _repo.Received(1).AddAsync(Arg.Is<ExchangeRate>(r =>
            r.Currency == "USD" && r.RateAgainstTry == 32.18m && r.TenantId == Guid.Empty),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upserts_existing_rate_by_updating_value()
    {
        var today = DateTime.SpecifyKind(new DateTime(2026, 6, 3), DateTimeKind.Utc);
        var existing = new ExchangeRate { Id = Guid.NewGuid(), Currency = "USD", RateAgainstTry = 31m, ValidOnDate = today };
        _client.FetchTodayAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new TcmbRate("USD", 32.5m, today) });
        _repo.GetAsync("USD", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new TcmbFxIngestJob(_client, _repo, _uow, CatalogSync(), NullLogger<TcmbFxIngestJob>.Instance);
        var count = await sut.RunAsync();

        count.Should().Be(1);
        existing.RateAgainstTry.Should().Be(32.5m);
        _repo.Received(1).Update(existing);
    }

    [Fact]
    public async Task No_op_when_feed_empty()
    {
        _client.FetchTodayAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<TcmbRate>());

        var sut = new TcmbFxIngestJob(_client, _repo, _uow, CatalogSync(), NullLogger<TcmbFxIngestJob>.Instance);
        var count = await sut.RunAsync();

        count.Should().Be(0);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Acquires_ingest_lock_and_dedups_a_repeated_currency_in_one_feed()
    {
        // Two concurrent runs used to race two identical inserts into a duplicate-key (23505); the
        // ingest now serialises via an advisory lock. A currency repeated within one feed must also
        // collapse to a single upsert (keeping the last value) rather than self-colliding.
        var today = DateTime.SpecifyKind(new DateTime(2026, 6, 3), DateTimeKind.Utc);
        _client.FetchTodayAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new TcmbRate("USD", 32.18m, today), new TcmbRate("USD", 33.00m, today) });
        _repo.GetAsync("USD", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns((ExchangeRate?)null);

        var sut = new TcmbFxIngestJob(_client, _repo, _uow, CatalogSync(), NullLogger<TcmbFxIngestJob>.Instance);
        var count = await sut.RunAsync();

        count.Should().Be(1);
        await _repo.Received(1).AcquireIngestLockAsync(Arg.Any<CancellationToken>());
        await _repo.Received(1).AddAsync(
            Arg.Is<ExchangeRate>(r => r.Currency == "USD" && r.RateAgainstTry == 33.00m),
            Arg.Any<CancellationToken>());
    }
}
