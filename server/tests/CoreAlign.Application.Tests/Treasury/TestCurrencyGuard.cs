using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests;

/// <summary>
/// A real <see cref="KnownCurrencyGuard"/> over a fixed catalogue, so validator tests exercise the
/// production rule instead of a stub that always says yes.
/// </summary>
public static class TestCurrencyGuard
{
    public static IKnownCurrencyGuard Accepting(params string[] codes)
    {
        var catalog = Substitute.For<ICurrencyCatalog>();
        catalog
            .ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(codes.Select(c => new Currency(c, c, null)).ToList());
        return new KnownCurrencyGuard(catalog);
    }
}
