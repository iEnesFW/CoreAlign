using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.EInvoice;

// UBL-TR carries the line quantity's unit as a UN/ECE Recommendation 20 code. The stored unit of
// measure is a human code (ADET, KILOGRAM, the legacy free text "Kg"/"pcs"/"M2", or a symbol), so it
// has to be translated before it reaches the XML. This is the ONLY place that translation happens.
public static class GibUnitCodeMap
{
    public const string DefaultCode = "C62";

    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ADET"] = "C62",
        ["AD"] = "C62",
        ["PCS"] = "C62",
        ["PC"] = "C62",
        ["PIECE"] = "C62",
        ["C62"] = "C62",

        // WHY these three resolve to "piece": the glass BOM emits one-off service lines with a
        // quantity of exactly 1 and a colloquial unit ("Waste allowance / 1 lot", "Transport /
        // 1 trip"). Rec 20 has no "lot"/"trip"; billing them as 1 piece is accurate for a lump-sum
        // line and is a code every integrator accepts, whereas passing the word through is refused.
        ["LOT"] = "C62",
        ["TRIP"] = "C62",
        ["SEFER"] = "C62",

        ["KILOGRAM"] = "KGM",
        ["KG"] = "KGM",
        ["KGM"] = "KGM",
        ["GRAM"] = "GRM",
        ["G"] = "GRM",
        ["GR"] = "GRM",
        ["GRM"] = "GRM",
        ["MILIGRAM"] = "MGM",
        ["MG"] = "MGM",
        ["MGM"] = "MGM",
        ["TON"] = "TNE",
        ["TNE"] = "TNE",
        ["ONS"] = "ONZ",
        ["OZ"] = "ONZ",
        ["ONZ"] = "ONZ",
        ["LIBRE"] = "LBR",
        ["LB"] = "LBR",
        ["LBR"] = "LBR",

        ["METRE"] = "MTR",
        ["MT"] = "MTR",
        ["M"] = "MTR",
        ["MTR"] = "MTR",
        ["METER"] = "MTR",
        ["SANTIMETRE"] = "CMT",
        ["CM"] = "CMT",
        ["CMT"] = "CMT",
        ["MILIMETRE"] = "MMT",
        ["MM"] = "MMT",
        ["MMT"] = "MMT",
        ["DESIMETRE"] = "DMT",
        ["DM"] = "DMT",
        ["DMT"] = "DMT",
        ["KILOMETRE"] = "KMT",
        ["KM"] = "KMT",
        ["KMT"] = "KMT",
        ["INC"] = "INH",
        ["IN"] = "INH",
        ["INH"] = "INH",
        ["FIT"] = "FOT",
        ["FT"] = "FOT",
        ["FOT"] = "FOT",
        ["YARDA"] = "YRD",
        ["YD"] = "YRD",
        ["YRD"] = "YRD",

        ["METREKARE"] = "MTK",
        ["M2"] = "MTK",
        ["M²"] = "MTK",
        ["SQM"] = "MTK",
        ["MTK"] = "MTK",
        ["SANTIMETREKARE"] = "CMK",
        ["CM2"] = "CMK",
        ["CM²"] = "CMK",
        ["CMK"] = "CMK",
        ["DESIMETREKARE"] = "DMK",
        ["DM2"] = "DMK",
        ["DM²"] = "DMK",
        ["DMK"] = "DMK",
        ["MILIMETREKARE"] = "MMK",
        ["MM2"] = "MMK",
        ["MM²"] = "MMK",
        ["MMK"] = "MMK",
        ["KILOMETREKARE"] = "KMK",
        ["KM2"] = "KMK",
        ["KM²"] = "KMK",
        ["KMK"] = "KMK",
        ["HEKTAR"] = "HAR",
        ["HA"] = "HAR",
        ["HAR"] = "HAR",
        ["DEKAR"] = "DAA",
        ["DAA"] = "DAA",
        ["DA"] = "DAA",

        ["METREKUP"] = "MTQ",
        ["M3"] = "MTQ",
        ["M³"] = "MTQ",
        ["MTQ"] = "MTQ",
        ["LITRE"] = "LTR",
        ["L"] = "LTR",
        ["LT"] = "LTR",
        ["LTR"] = "LTR",
        ["MILILITRE"] = "MLT",
        ["ML"] = "MLT",
        ["MLT"] = "MLT",
        ["SANTILITRE"] = "CLT",
        ["CL"] = "CLT",
        ["CLT"] = "CLT",
        ["GALON"] = "GLL",
        ["GAL"] = "GLL",
        ["GLL"] = "GLL",

        ["PAKET"] = "PK",
        ["PK"] = "PK",
        ["KUTU"] = "BX",
        ["BX"] = "BX",
        ["KOLI"] = "CT",
        ["CT"] = "CT",
        ["PALET"] = "PF",
        ["PF"] = "PF",
        ["ROLE"] = "RO",
        ["RULO"] = "RO",
        ["RO"] = "RO",
        ["TAKIM"] = "SET",
        ["SET"] = "SET",
        ["CIFT"] = "PR",
        ["PR"] = "PR",
        ["DUZINE"] = "DZN",
        ["DZ"] = "DZN",
        ["DZN"] = "DZN",

        ["SAAT"] = "HUR",
        ["HUR"] = "HUR",
        ["DAKIKA"] = "MIN",
        ["DK"] = "MIN",
        ["MIN"] = "MIN",
        ["SANIYE"] = "SEC",
        ["SN"] = "SEC",
        ["SEC"] = "SEC",
        ["GUN"] = "DAY",
        ["DAY"] = "DAY",
        ["HOUR"] = "HUR",
        ["SECOND"] = "SEC",
        ["MINUTE"] = "MIN",
        ["AY"] = "MON",
        ["MO"] = "MON",
        ["MONTH"] = "MON",
        ["MON"] = "MON",
        ["HAFTA"] = "WEE",
        ["WEEK"] = "WEE",
        ["WEE"] = "WEE",
        ["YIL"] = "ANN",
        ["YEAR"] = "ANN",
        ["ANN"] = "ANN",
    };

    public static bool TryResolve(string? unitOfMeasure, out string gibUnitCode)
    {
        if (string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            gibUnitCode = DefaultCode;
            return true;
        }
        return Map.TryGetValue(Normalize(unitOfMeasure), out gibUnitCode!);
    }

    // WHY: silently falling back to C62 ("piece") for an unmapped unit turns a kilogram or a square
    // metre into a piece count on a legally binding document, so an unknown unit must stop the send.
    public static string Resolve(string? unitOfMeasure) =>
        TryResolve(unitOfMeasure, out var code)
            ? code
            : throw new UnmappedUnitCodeException(unitOfMeasure!);

    public static IReadOnlyCollection<string> KnownUnits => Map.Keys;

    private static string Normalize(string value) =>
        value.Trim().Replace("İ", "I", StringComparison.Ordinal).Replace("ı", "i", StringComparison.Ordinal);
}
