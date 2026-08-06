using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Treasury;

public class CurrencyCatalogSyncTests
{
    private readonly ICurrencyCatalog _catalog = Substitute.For<ICurrencyCatalog>();
    private readonly List<Currency> _added = new();

    private CurrencyCatalogSync Build(params Currency[] existing)
    {
        _catalog.ListAllAsync(Arg.Any<CancellationToken>()).Returns(existing.ToList());
        _catalog
            .AddAsync(Arg.Do<Currency>(c => _added.Add(c)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return new CurrencyCatalogSync(_catalog);
    }

    [Fact]
    public async Task A_currency_the_feed_publishes_becomes_pickable()
    {
        var sut = Build(new Currency("TRY", "Türk Lirası", "₺"));

        var added = await sut.EnsureAsync(new[] { "TRY", "USD", "JPY" });

        added.Should().Be(2);
        _added.Select(c => c.Code).Should().BeEquivalentTo(new[] { "USD", "JPY" });
        _added.Single(c => c.Code == "JPY").Name.Should().Be("Japon Yeni");
        _added.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task An_unknown_code_still_enters_the_catalogue_under_its_own_code()
    {
        var sut = Build();

        await sut.EnsureAsync(new[] { "ZZZ" });

        _added.Single().Code.Should().Be("ZZZ");
        _added.Single().Name.Should().Be("ZZZ");
    }

    // WHY this matters: the nightly job would otherwise undo an operator's decision every morning.
    [Fact]
    public async Task A_currency_an_admin_switched_off_is_not_re_activated_by_the_feed()
    {
        var disabled = new Currency("USD", "ABD Doları", "$", isActive: false);
        var sut = Build(disabled);

        await sut.EnsureAsync(new[] { "USD" });

        disabled.IsActive.Should().BeFalse();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task A_name_a_human_edited_survives_the_feed()
    {
        var renamed = new Currency("JPY", "Yen (merkez ofis)", "¥");
        var sut = Build(renamed);

        await sut.EnsureAsync(new[] { "JPY" });

        renamed.Name.Should().Be("Yen (merkez ofis)");
    }

    [Fact]
    public async Task A_placeholder_name_is_upgraded_once_the_feed_teaches_us_the_real_one()
    {
        var placeholder = new Currency("RON", "RON", null);
        var sut = Build(placeholder);

        await sut.EnsureAsync(new[] { "RON" });

        placeholder.Name.Should().Be("Rumen Leyi");
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData("  eur ", "EUR")]
    public async Task Feed_codes_are_normalised_before_they_are_compared(string feed, string expected)
    {
        var sut = Build();

        await sut.EnsureAsync(new[] { feed });

        _added.Single().Code.Should().Be(expected);
    }

    [Fact]
    public async Task A_duplicate_in_one_feed_creates_one_row()
    {
        var sut = Build();

        var added = await sut.EnsureAsync(new[] { "USD", "usd", "USD" });

        added.Should().Be(1);
        _added.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("TOOLONG")]
    [InlineData("XY")]
    public async Task A_code_that_cannot_be_an_iso_4217_code_is_ignored(string bogus)
    {
        var sut = Build();

        var added = await sut.EnsureAsync(new[] { bogus });

        added.Should().Be(0);
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task An_empty_feed_touches_nothing()
    {
        var sut = Build();

        (await sut.EnsureAsync(Array.Empty<string>())).Should().Be(0);
        await _catalog.DidNotReceive().ListAllAsync(Arg.Any<CancellationToken>());
    }
}
