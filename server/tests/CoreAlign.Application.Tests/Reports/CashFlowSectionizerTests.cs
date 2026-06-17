using CoreAlign.Application.Reports.Common;

namespace CoreAlign.Application.Tests.Reports;

public class CashFlowSectionizerTests
{
    [Theory]
    [InlineData("120")]
    [InlineData("121")]
    [InlineData("320")]
    [InlineData("321")]
    [InlineData("600")]
    [InlineData("621")]
    [InlineData("632")]
    [InlineData("191")]
    [InlineData("391")]
    [InlineData("360")]
    public void Operating_counterparts_map_to_Operating(string code)
    {
        CashFlowSectionizer.SectionForCounterpart(code).Should().Be(CashFlowSection.Operating);
    }

    [Theory]
    [InlineData("250")]
    [InlineData("252")]
    [InlineData("253")]
    [InlineData("254")]
    [InlineData("255")]
    [InlineData("260")]
    public void FixedAsset_counterparts_map_to_Investing(string code)
    {
        CashFlowSectionizer.SectionForCounterpart(code).Should().Be(CashFlowSection.Investing);
    }

    [Theory]
    [InlineData("300")]
    [InlineData("303")]
    [InlineData("400")]
    [InlineData("331")]
    [InlineData("500")]
    [InlineData("540")]
    [InlineData("590")]
    public void Borrowing_and_equity_counterparts_map_to_Financing(string code)
    {
        CashFlowSectionizer.SectionForCounterpart(code).Should().Be(CashFlowSection.Financing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("799")]
    public void Unknown_or_missing_counterpart_defaults_to_Operating(string? code)
    {
        CashFlowSectionizer.SectionForCounterpart(code).Should().Be(CashFlowSection.Operating);
    }

    [Fact]
    public void Payable_320_stays_Operating_even_though_partner_account_330_is_Financing()
    {
        CashFlowSectionizer.SectionForCounterpart("320").Should().Be(CashFlowSection.Operating);
        CashFlowSectionizer.SectionForCounterpart("331").Should().Be(CashFlowSection.Financing);
    }
}
