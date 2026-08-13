using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Treasury;

/// <summary>
/// Shape-only validation ("three uppercase letters") accepts TRL, XXX or any decommissioned code;
/// the document is then created and the mistake surfaces much later as a missing FX rate or an
/// amount that was never converted. The catalogue is the only place that knows what is real.
/// </summary>
public class KnownCurrencyGuardTests
{
    private readonly ICurrencyCatalog _catalog = Substitute.For<ICurrencyCatalog>();

    private KnownCurrencyGuard Build(params Currency[] rows)
    {
        _catalog.ListAllAsync(Arg.Any<CancellationToken>()).Returns(rows.ToList());
        return new KnownCurrencyGuard(_catalog);
    }

    [Fact]
    public async Task A_code_in_the_catalogue_is_usable()
    {
        var guard = Build(new Currency("TRY", "Türk Lirası", "₺"));

        (await guard.IsUsableAsync("TRY")).Should().BeTrue();
    }

    [Fact]
    public async Task A_typo_that_is_shaped_like_a_currency_is_refused()
    {
        var guard = Build(new Currency("TRY", "Türk Lirası", "₺"));

        (await guard.IsUsableAsync("TRL")).Should().BeFalse();
    }

    [Fact]
    public async Task Casing_and_padding_do_not_decide_the_answer()
    {
        var guard = Build(new Currency("EUR", "Euro", "€"));

        (await guard.IsUsableAsync(" eur ")).Should().BeTrue();
    }

    [Fact]
    public async Task A_currency_an_admin_switched_off_is_refused()
    {
        var retired = new Currency("XAU", "Altın", null);
        retired.SetActive(false);
        var guard = Build(new Currency("TRY", "Türk Lirası", "₺"), retired);

        (await guard.IsUsableAsync("XAU")).Should().BeFalse();
    }

    /// <summary>An empty code is NotEmpty's job — this guard must not double-report it.</summary>
    [Fact]
    public async Task An_empty_code_is_left_to_the_required_rule()
    {
        var guard = Build(new Currency("TRY", "Türk Lirası", "₺"));

        (await guard.IsUsableAsync(null)).Should().BeTrue();
        (await guard.IsUsableAsync("   ")).Should().BeTrue();
    }

    /// <summary>
    /// A catalogue that has never been populated must not take the whole ERP down: refusing every
    /// currency would block invoicing on a fresh install before the FX feed has run once.
    /// </summary>
    [Fact]
    public async Task An_empty_catalogue_accepts_anything()
    {
        var guard = Build();

        (await guard.IsUsableAsync("TRY")).Should().BeTrue();
        (await guard.IsUsableAsync("ZZZ")).Should().BeTrue();
    }

    [Fact]
    public async Task The_catalogue_is_read_once_per_scope()
    {
        var guard = Build(new Currency("TRY", "Türk Lirası", "₺"));

        await guard.IsUsableAsync("TRY");
        await guard.IsUsableAsync("EUR");
        await guard.IsUsableAsync("USD");

        await _catalog.Received(1).ListAllAsync(Arg.Any<CancellationToken>());
    }
}
