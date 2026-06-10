using System.Text.RegularExpressions;

namespace CoreAlign.Application.Common.Validation;

public sealed record CountryAddressRule(
    string CountryCode,
    Regex? PostalCodePattern,
    bool RequiresState,
    int MinPhoneDigits,
    int MaxPhoneDigits);

public static class CountryAddressRules
{
    private const RegexOptions Opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly Dictionary<string, CountryAddressRule> Rules =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TR"] = new("TR", new Regex(@"^\d{5}$", Opts), RequiresState: false, MinPhoneDigits: 10, MaxPhoneDigits: 12),
            ["US"] = new("US", new Regex(@"^\d{5}(-\d{4})?$", Opts), RequiresState: true, MinPhoneDigits: 10, MaxPhoneDigits: 11),
            ["CA"] = new("CA", new Regex(@"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z][ ]?\d[ABCEGHJ-NPRSTV-Z]\d$", Opts | RegexOptions.IgnoreCase), RequiresState: true, MinPhoneDigits: 10, MaxPhoneDigits: 11),
            ["GB"] = new("GB", new Regex(@"^[A-Z]{1,2}\d[A-Z\d]?[ ]?\d[A-Z]{2}$", Opts | RegexOptions.IgnoreCase), RequiresState: false, MinPhoneDigits: 10, MaxPhoneDigits: 11),
            ["DE"] = new("DE", new Regex(@"^\d{5}$", Opts), RequiresState: false, MinPhoneDigits: 7, MaxPhoneDigits: 13),
            ["FR"] = new("FR", new Regex(@"^\d{5}$", Opts), RequiresState: false, MinPhoneDigits: 9, MaxPhoneDigits: 10),
            ["NL"] = new("NL", new Regex(@"^\d{4}[ ]?[A-Z]{2}$", Opts | RegexOptions.IgnoreCase), RequiresState: false, MinPhoneDigits: 9, MaxPhoneDigits: 10),
            ["IT"] = new("IT", new Regex(@"^\d{5}$", Opts), RequiresState: false, MinPhoneDigits: 9, MaxPhoneDigits: 11),
            ["ES"] = new("ES", new Regex(@"^\d{5}$", Opts), RequiresState: false, MinPhoneDigits: 9, MaxPhoneDigits: 9),
            ["AU"] = new("AU", new Regex(@"^\d{4}$", Opts), RequiresState: true, MinPhoneDigits: 9, MaxPhoneDigits: 10),
            ["JP"] = new("JP", new Regex(@"^\d{3}-?\d{4}$", Opts), RequiresState: false, MinPhoneDigits: 10, MaxPhoneDigits: 11),
            ["CH"] = new("CH", new Regex(@"^\d{4}$", Opts), RequiresState: false, MinPhoneDigits: 9, MaxPhoneDigits: 10),
            ["AT"] = new("AT", new Regex(@"^\d{4}$", Opts), RequiresState: false, MinPhoneDigits: 8, MaxPhoneDigits: 13),
            ["BE"] = new("BE", new Regex(@"^\d{4}$", Opts), RequiresState: false, MinPhoneDigits: 8, MaxPhoneDigits: 10),
            ["CN"] = new("CN", new Regex(@"^\d{6}$", Opts), RequiresState: false, MinPhoneDigits: 10, MaxPhoneDigits: 12),
            ["BR"] = new("BR", new Regex(@"^\d{5}-?\d{3}$", Opts), RequiresState: false, MinPhoneDigits: 10, MaxPhoneDigits: 11),
            ["MX"] = new("MX", new Regex(@"^\d{5}$", Opts), RequiresState: false, MinPhoneDigits: 10, MaxPhoneDigits: 10),
        };

    public static bool IsKnown(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return false;
        return Rules.ContainsKey(countryCode.Trim());
    }

    public static CountryAddressRule? Get(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return null;
        return Rules.TryGetValue(countryCode.Trim(), out var rule) ? rule : null;
    }

    public static bool IsValidPostalCode(string? countryCode, string? postalCode)
    {
        var rule = Get(countryCode);
        if (rule is null) return true;
        if (rule.PostalCodePattern is null) return true;
        if (string.IsNullOrWhiteSpace(postalCode)) return false;
        return rule.PostalCodePattern.IsMatch(postalCode.Trim());
    }

    public static bool RequiresState(string? countryCode)
    {
        var rule = Get(countryCode);
        return rule?.RequiresState ?? false;
    }

    public static bool IsValidPhoneNumber(string? countryCode, string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var rule = Get(countryCode);
        if (rule is null) return true;
        var digits = CountDigits(phone);
        return digits >= rule.MinPhoneDigits && digits <= rule.MaxPhoneDigits;
    }

    private static int CountDigits(string value)
    {
        var count = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] >= '0' && value[i] <= '9') count++;
        }
        return count;
    }
}
