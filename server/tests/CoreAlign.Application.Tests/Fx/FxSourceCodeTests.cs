using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Fx;

public class FxSourceCodeTests
{
    [Theory]
    [InlineData("TCMB", FxSource.Tcmb)]
    [InlineData("tcmb", FxSource.Tcmb)]
    [InlineData("ECB", FxSource.Ecb)]
    [InlineData("MANUAL", FxSource.Manual)]
    [InlineData("TENANT_OVERRIDE", FxSource.TenantOverride)]
    [InlineData(null, FxSource.Tcmb)]
    [InlineData("", FxSource.Tcmb)]
    [InlineData("unknown", FxSource.Tcmb)]
    public void Parse_NormalizesToExpectedSource(string? input, FxSource expected)
    {
        Assert.Equal(expected, FxSourceCodes.Parse(input));
    }

    [Theory]
    [InlineData(FxSource.Tcmb, "TCMB")]
    [InlineData(FxSource.Ecb, "ECB")]
    [InlineData(FxSource.Manual, "MANUAL")]
    [InlineData(FxSource.TenantOverride, "TENANT_OVERRIDE")]
    public void ToCode_ReturnsCanonicalCode(FxSource source, string expected)
    {
        Assert.Equal(expected, FxSourceCodes.ToCode(source));
    }

    [Fact]
    public void TenantFxPreferenceSnapshot_ReturnsDefault_WhenNoOverrides()
    {
        var snap = new CoreAlign.Application.Fx.TenantFxPreferenceSnapshot(
            FxSource.Tcmb,
            new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(FxSource.Tcmb, snap.ResolveSourceFor("USD"));
    }

    [Fact]
    public void TenantFxPreferenceSnapshot_RespectsPerCurrencyOverride()
    {
        var overrides = new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase)
        {
            ["EUR"] = FxSource.Ecb,
        };
        var snap = new CoreAlign.Application.Fx.TenantFxPreferenceSnapshot(FxSource.Tcmb, overrides);
        Assert.Equal(FxSource.Ecb, snap.ResolveSourceFor("EUR"));
        Assert.Equal(FxSource.Tcmb, snap.ResolveSourceFor("USD"));
    }
}
