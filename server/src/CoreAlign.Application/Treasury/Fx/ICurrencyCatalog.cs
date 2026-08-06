using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Treasury.Fx;

public interface ICurrencyCatalog
{
    Task<IReadOnlyList<Currency>> ListAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Currency currency, CancellationToken cancellationToken = default);
    void Update(Currency currency);
}

// The pickable currency list DERIVES from the FX feed: every code the daily bulletin publishes
// becomes a catalogue row, so a rate can never exist for a currency the user is unable to select.
// Manual rows are still first-class — this only ever ADDS what the feed introduced and never
// deletes, never re-activates something an admin switched off, and never overwrites a name a human
// has already edited (Currency.AdoptFeedName).
public sealed class CurrencyCatalogSync
{
    private readonly ICurrencyCatalog _catalog;

    public CurrencyCatalogSync(ICurrencyCatalog catalog) => _catalog = catalog;

    public async Task<int> EnsureAsync(IEnumerable<string> feedCodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feedCodes);

        var codes = feedCodes
            .Select(Currency.Normalize)
            .Where(code => code.Length is 3)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (codes.Count == 0) return 0;

        var existing = (await _catalog.ListAllAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var code in codes)
        {
            var (name, symbol) = CurrencyCatalogNames.Resolve(code);
            if (existing.TryGetValue(code, out var current))
            {
                current.AdoptFeedName(name, symbol);
                _catalog.Update(current);
                continue;
            }
            await _catalog.AddAsync(new Currency(code, name, symbol), cancellationToken).ConfigureAwait(false);
            added++;
        }
        return added;
    }
}
