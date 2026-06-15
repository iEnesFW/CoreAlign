using CoreAlign.Application.Treasury.Fx;

namespace CoreAlign.Application.Tests.Treasury;

public class FxRevaluationTests
{
    [Fact]
    public void Receivable_in_appreciating_currency_creates_gain()
    {
        var balances = new[] { new OpenForeignBalance("USD", ForeignAmount: 1000m, BookedRate: 30m, IsReceivable: true, TenantId: Guid.NewGuid()) };
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["USD"] = 32m };

        var rows = FxRevaluation.Compute(balances, rates);

        rows.Should().ContainSingle();
        rows[0].IsGain.Should().BeTrue();
        rows[0].DeltaTry.Should().Be(2000m, because: "1000 USD * (32-30) TRY = 2000 TRY gain");
    }

    [Fact]
    public void Receivable_in_depreciating_currency_creates_loss()
    {
        var balances = new[] { new OpenForeignBalance("EUR", 500m, 35m, IsReceivable: true, TenantId: Guid.NewGuid()) };
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["EUR"] = 33m };

        var rows = FxRevaluation.Compute(balances, rates);

        rows.Should().ContainSingle();
        rows[0].IsGain.Should().BeFalse();
        rows[0].DeltaTry.Should().Be(1000m);
    }

    [Fact]
    public void Payable_in_appreciating_currency_creates_loss()
    {
        var balances = new[] { new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: false, TenantId: Guid.NewGuid()) };
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["USD"] = 32m };

        var rows = FxRevaluation.Compute(balances, rates);

        rows.Should().ContainSingle().Which.IsGain.Should().BeFalse();
        rows[0].DeltaTry.Should().Be(2000m);
    }

    [Fact]
    public void Skips_currencies_with_no_current_rate_available()
    {
        var tenantId = Guid.NewGuid();
        var balances = new[]
        {
            new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId: tenantId),
            new OpenForeignBalance("XYZ", 500m, 1m, IsReceivable: true, TenantId: tenantId),
        };
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["USD"] = 32m };

        var rows = FxRevaluation.Compute(balances, rates);
        rows.Should().ContainSingle().Which.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_delta_rows_are_dropped()
    {
        var balances = new[] { new OpenForeignBalance("USD", 100m, 30m, IsReceivable: true, TenantId: Guid.NewGuid()) };
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["USD"] = 30m };
        FxRevaluation.Compute(balances, rates).Should().BeEmpty();
    }
}
