namespace CoreAlign.Application.Reports.Common;

public static class CashFlowSection
{
    public const string Operating = "Operating";
    public const string Investing = "Investing";
    public const string Financing = "Financing";
}

/// <summary>
/// Maps a cash movement to a cash-flow statement section by inspecting the
/// COUNTERPART (non-cash) leg's TDHP account code. Day-to-day trade flows
/// (revenue/expense/COGS, receivables 12x, payables 32x/42x, VAT/tax 19x/36x/39x)
/// are Operating; fixed-asset &amp; long-term investment movements (24x/25x/26x)
/// are Investing; borrowings and equity (30x/40x bank loans, 33x partner
/// current accounts, 5xx capital/dividends) are Financing. Unknown counterparts
/// default to Operating — the conservative bucket for unclassified trade flow.
/// </summary>
public static class CashFlowSectionizer
{
    public static string SectionForCounterpart(string? counterpartAccountCode)
    {
        if (string.IsNullOrWhiteSpace(counterpartAccountCode))
        {
            return CashFlowSection.Operating;
        }

        var code = counterpartAccountCode.Trim();

        return PrefixMatches(code, "24", "25", "26")
            ? CashFlowSection.Investing
            : PrefixMatches(code, "30", "40", "33", "5")
                ? CashFlowSection.Financing
                : CashFlowSection.Operating;
    }

    private static bool PrefixMatches(string code, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (code.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}
