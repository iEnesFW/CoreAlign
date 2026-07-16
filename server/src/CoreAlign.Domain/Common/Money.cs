namespace CoreAlign.Domain.Common;

/// <summary>
/// Centralized rounding policy for all monetary amounts.
///
/// Contract:
///   • Monetary amount columns use <c>numeric(18,4)</c> in PostgreSQL — i.e. 4 fractional digits.
///   • Exchange rates use <c>numeric(18,6)</c> — 6 fractional digits.
///   • Percent values use <c>numeric(6,3)</c> — 3 fractional digits.
///
/// All in-memory calculations that need to be persisted should be rounded with the
/// helpers below to keep server, database, and printed invoice totals byte-for-byte
/// consistent. The mode is <see cref="MidpointRounding.ToEven"/> ("banker's rounding")
/// to match .NET <c>Math.Round</c> defaults and avoid systematic upward bias.
/// </summary>
public static class Money
{
    /// <summary>4 fractional digits — same as the <c>numeric(18,4)</c> column.</summary>
    public const int AmountScale = 4;

    /// <summary>6 fractional digits — same as the <c>numeric(18,6)</c> column.</summary>
    public const int RateScale = 6;

    /// <summary>3 fractional digits — same as the <c>numeric(6,3)</c> column.</summary>
    public const int PercentScale = 3;

    /// <summary>Banker's rounding to match .NET defaults and avoid upward bias.</summary>
    public const MidpointRounding RoundingMode = MidpointRounding.ToEven;

    /// <summary>Round a monetary amount to 4 fractional digits.</summary>
    public static decimal RoundAmount(decimal value) =>
        Math.Round(value, AmountScale, RoundingMode);

    /// <summary>Round an exchange rate to 6 fractional digits.</summary>
    public static decimal RoundRate(decimal value) =>
        Math.Round(value, RateScale, RoundingMode);

    /// <summary>Round a percent value to 3 fractional digits.</summary>
    public static decimal RoundPercent(decimal value) =>
        Math.Round(value, PercentScale, RoundingMode);

    /// <summary>
    /// Apply <paramref name="ratePercent"/> (e.g. 18 for 18%) to <paramref name="amount"/>
    /// and return the rounded tax amount. Returns 0 when the rate is 0 or negative.
    /// </summary>
    public static decimal ApplyPercent(decimal amount, decimal ratePercent)
    {
        if (ratePercent <= 0m) return 0m;
        return RoundAmount(amount * ratePercent / 100m);
    }
}
