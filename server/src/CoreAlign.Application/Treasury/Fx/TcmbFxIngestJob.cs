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
    private readonly ILogger<TcmbFxIngestJob> _logger;

    public TcmbFxIngestJob(ITcmbFxClient client, IExchangeRateRepository repo, IUnitOfWork uow, ILogger<TcmbFxIngestJob> logger)
    {
        _client = client;
        _repo = repo;
        _uow = uow;
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

        var upserted = 0;
        foreach (var rate in rates)
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

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("TCMB FX feed upserted {Count} rates.", upserted);
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
