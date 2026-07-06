using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities.Treasury;
using MediatR;

namespace CoreAlign.Application.Treasury.Fx;

public sealed record ExchangeRateDto(
    Guid Id,
    string Currency,
    decimal RateAgainstTry,
    DateTime ValidOnDate,
    string Source,
    DateTime FetchedAtUtc);

public sealed record ListExchangeRatesQuery(DateTime? FromDate, DateTime? ToDate, string? Currency)
    : IRequest<IReadOnlyList<ExchangeRateDto>>;

public sealed record TriggerTcmbFxPollCommand : IRequest<int>;

public interface ITcmbFxClient
{
    Task<IReadOnlyList<TcmbRate>> FetchTodayAsync(CancellationToken ct);
}

public sealed record TcmbRate(string Currency, decimal ForexSelling, DateTime ValidOnDate);

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetAsync(string currency, DateTime validOnDate, CancellationToken ct);
    Task<IReadOnlyList<ExchangeRate>> ListAsync(DateTime? from, DateTime? to, string? currency, CancellationToken ct);
    Task<IReadOnlyList<ExchangeRate>> GetLatestPerCurrencyOnOrBeforeAsync(DateTime asOf, CancellationToken ct);
    Task AddAsync(ExchangeRate rate, CancellationToken ct);
    void Update(ExchangeRate rate);

    // Serialises concurrent TCMB ingest runs so the check-then-insert upsert cannot race two
    // runs into a duplicate (tenant_id, currency, valid_on_date) key. Must be called inside a
    // transaction (the xact lock releases on commit/rollback).
    Task AcquireIngestLockAsync(CancellationToken ct);
}

internal static class ExchangeRateMapper
{
    public static ExchangeRateDto ToDto(ExchangeRate r) =>
        new(r.Id, r.Currency, r.RateAgainstTry, r.ValidOnDate, r.Source, r.FetchedAtUtc);
}

public sealed class ListExchangeRatesHandler : IRequestHandler<ListExchangeRatesQuery, IReadOnlyList<ExchangeRateDto>>
{
    private readonly IExchangeRateRepository _repo;
    public ListExchangeRatesHandler(IExchangeRateRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ExchangeRateDto>> Handle(ListExchangeRatesQuery query, CancellationToken cancellationToken)
    {
        var items = await _repo.ListAsync(query.FromDate, query.ToDate, query.Currency, cancellationToken);
        return items.Select(ExchangeRateMapper.ToDto).ToArray();
    }
}
