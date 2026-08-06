namespace CoreAlign.Application.Treasury.Fx;

// The FX feed carries a rate and a code but no display name (TcmbRate is code + rate + date), so a
// currency the feed introduces needs its name from somewhere. This is that somewhere: the ISO 4217
// codes the TCMB daily bulletin actually publishes, named the way the existing seed names them.
// A code that is not here still enters the catalogue — under its own code — and an admin can rename it.
public static class CurrencyCatalogNames
{
    private static readonly Dictionary<string, (string Name, string? Symbol)> Known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TRY"] = ("Türk Lirası", "₺"),
            ["USD"] = ("ABD Doları", "$"),
            ["EUR"] = ("Euro", "€"),
            ["GBP"] = ("İngiliz Sterlini", "£"),
            ["CHF"] = ("İsviçre Frangı", "CHF"),
            ["JPY"] = ("Japon Yeni", "¥"),
            ["AUD"] = ("Avustralya Doları", "A$"),
            ["CAD"] = ("Kanada Doları", "C$"),
            ["DKK"] = ("Danimarka Kronu", "kr"),
            ["NOK"] = ("Norveç Kronu", "kr"),
            ["SEK"] = ("İsveç Kronu", "kr"),
            ["RUB"] = ("Rus Rublesi", "₽"),
            ["CNY"] = ("Çin Yuanı", "¥"),
            ["KRW"] = ("Güney Kore Wonu", "₩"),
            ["PKR"] = ("Pakistan Rupisi", "₨"),
            ["RON"] = ("Rumen Leyi", "lei"),
            ["BGN"] = ("Bulgar Levası", "лв"),
            ["AED"] = ("BAE Dirhemi", "د.إ"),
            ["SAR"] = ("Suudi Riyali", "﷼"),
            ["QAR"] = ("Katar Riyali", "﷼"),
            ["KWD"] = ("Kuveyt Dinarı", "د.ك"),
            ["AZN"] = ("Azerbaycan Manatı", "₼"),
            ["KZT"] = ("Kazakistan Tengesi", "₸"),
            ["IRR"] = ("İran Riyali", "﷼"),
            ["XDR"] = ("SDR (Özel Çekme Hakkı)", null),
        };

    public static (string Name, string? Symbol) Resolve(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        return Known.TryGetValue(normalized, out var known) ? known : (normalized, null);
    }

    public static bool IsKnown(string code) => Known.ContainsKey((code ?? string.Empty).Trim());
}
