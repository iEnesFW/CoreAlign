using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests;

/// <summary>
/// A real <see cref="KnownCurrencyGuard"/> over a fixed catalogue, so validator tests exercise the
/// production rule instead of a stub that always says yes.
/// </summary>
public static class TestCurrencyGuard
{
    public static IKnownCurrencyGuard Accepting(params string[] codes) =>
        new KnownCurrencyGuard(new FixedCatalog(codes));

    // WHY a hand-written fake and not a substitute: this is built in test field initialisers, and a
    // queued Arg matcher there can be consumed by whichever test class happens to run alongside,
    // leaving the catalogue empty — which the guard reads as "feed never ran, accept anything".
    private sealed class FixedCatalog : ICurrencyCatalog
    {
        private readonly IReadOnlyList<Currency> _rows;

        public FixedCatalog(IEnumerable<string> codes) =>
            _rows = codes.Select(c => new Currency(c, c)).ToList();

        public Task<IReadOnlyList<Currency>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows);

        public Task AddAsync(Currency currency, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Update(Currency currency) { }
    }
}
