using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Treasury.Fx;

public sealed class TcmbFxIngestJob
{
    private readonly ITcmbFxClient _client;
    private readonly IExchangeRateRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly CurrencyCatalogSync _catalogSync;
    private readonly ILogger<TcmbFxIngestJob> _logger;

    public TcmbFxIngestJob(
        ITcmbFxClient client,
        IExchangeRateRepository repo,
        IUnitOfWork uow,
        CurrencyCatalogSync catalogSync,
        ILogger<TcmbFxIngestJob> logger)
    {
        _client = client;
        _repo = repo;
        _uow = uow;
        _catalogSync = catalogSync;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var rates = await _client.FetchTodayAsync(cancellationToken);
        if (rates.Count == 0)
        {
            _logger.LogInformation("TCMB FX feed returned no rates.");
            return 0;
        }

        // Guard against a currency appearing twice in one feed (would self-collide on the key).
        var deduped = rates
            .GroupBy(r => (r.Currency, Date: r.ValidOnDate.Date))
            .Select(g => g.Last())
            .ToList();

        // Serialise concurrent ingest runs (a Hangfire missed-run can overlap another trigger) inside
        // one transaction so the check-then-insert below cannot race two runs into a duplicate-key on
        // (tenant_id, currency, valid_on_date). The advisory lock releases on commit/rollback; a second
        // run blocks until the first commits, then finds the rows and updates instead of inserting.
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);
        await _repo.AcquireIngestLockAsync(cancellationToken);

        var upserted = 0;
        foreach (var rate in deduped)
        {
            var existing = await _repo.GetAsync(rate.Currency, rate.ValidOnDate.Date, cancellationToken);
            if (existing is null)
            {
                await _repo.AddAsync(new ExchangeRate
                {
                    Id = Guid.NewGuid(),
                    TenantId = Guid.Empty,
                    Currency = rate.Currency,
                    RateAgainstTry = rate.ForexSelling,
                    ValidOnDate = DateTime.SpecifyKind(rate.ValidOnDate.Date, DateTimeKind.Utc),
                    Source = "TCMB",
                    FetchedAtUtc = DateTime.UtcNow,
                }, cancellationToken);
            }
            else
            {
                existing.RateAgainstTry = rate.ForexSelling;
                existing.FetchedAtUtc = DateTime.UtcNow;
                _repo.Update(existing);
            }
            upserted++;
        }

        // The pickable currency list derives from this feed, so a code that just got a rate must
        // also exist in the catalogue — inside the same transaction as the rates that justify it.
        var addedCurrencies = await _catalogSync
            .EnsureAsync(deduped.Select(r => r.Currency), cancellationToken)
            .ConfigureAwait(false);

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        _logger.LogInformation(
            "TCMB FX feed upserted {Count} rates and introduced {Added} currencies.",
            upserted,
            addedCurrencies);
        return upserted;
    }
}

public sealed class TriggerTcmbFxPollHandler : IRequestHandler<TriggerTcmbFxPollCommand, int>
{
    private readonly TcmbFxIngestJob _job;
    public TriggerTcmbFxPollHandler(TcmbFxIngestJob job) => _job = job;
    public Task<int> Handle(TriggerTcmbFxPollCommand request, CancellationToken cancellationToken) =>
        _job.RunAsync(cancellationToken);
}
