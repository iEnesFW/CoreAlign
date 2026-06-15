using System.Globalization;

namespace CoreAlign.Application.Imports.Common;

public static class ParsingHelpers
{
    public static decimal ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        if (decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
        return 0m;
    }

    public static bool ParseBool(string? raw, bool fallback = false)
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        var trimmed = raw.Trim();
        if (bool.TryParse(trimmed, out var b)) return b;
        return trimmed switch
        {
            "1" or "yes" or "Y" or "y" or "true" or "TRUE" or "evet" or "EVET" => true,
            "0" or "no" or "N" or "n" or "false" or "FALSE" or "hayir" or "HAYIR" => false,
            _ => fallback
        };
    }
}
