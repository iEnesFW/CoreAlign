namespace CoreAlign.Application.MasterData;

public static class StandardUnitsOfMeasureSeed
{
    public sealed record Entry(
        string Code,
        string Name,
        string Symbol,
        string? BaseCode,
        decimal ConversionFactor,
        int DecimalPlaces);

    // Conversion factors are expressed relative to the category's base unit
    // (the entry whose BaseCode is null and ConversionFactor is 1).
    public static readonly IReadOnlyList<Entry> Entries = new List<Entry>
    {
        // Sayım / paketleme
        new("ADET", "Adet", "ad", null, 1m, 0),
        new("DZ", "Düzine", "dz", "ADET", 12m, 0),
        new("CIFT", "Çift", "çift", "ADET", 2m, 0),
        new("PK", "Paket", "pk", null, 1m, 0),
        new("KUTU", "Kutu", "kt", null, 1m, 0),
        new("KOLI", "Koli", "koli", null, 1m, 0),
        new("PALET", "Palet", "plt", null, 1m, 0),
        new("TAKIM", "Takım", "tk", null, 1m, 0),
        new("RULO", "Rulo", "rl", null, 1m, 0),

        // Uzunluk (taban: METRE)
        new("MT", "Metre", "m", null, 1m, 2),
        new("MM", "Milimetre", "mm", "MT", 0.001m, 0),
        new("CM", "Santimetre", "cm", "MT", 0.01m, 1),
        new("DM", "Desimetre", "dm", "MT", 0.1m, 2),
        new("KM", "Kilometre", "km", "MT", 1000m, 3),
        new("INC", "İnç", "in", "MT", 0.0254m, 2),
        new("FIT", "Fit", "ft", "MT", 0.3048m, 2),
        new("YD", "Yarda", "yd", "MT", 0.9144m, 2),

        // Ağırlık (taban: KILOGRAM)
        new("KG", "Kilogram", "kg", null, 1m, 3),
        new("GR", "Gram", "g", "KG", 0.001m, 1),
        new("MG", "Miligram", "mg", "KG", 0.000001m, 0),
        new("TON", "Ton", "t", "KG", 1000m, 3),
        new("LB", "Libre", "lb", "KG", 0.453592m, 3),
        new("ONS", "Ons", "oz", "KG", 0.0283495m, 3),

        // Hacim (taban: LITRE)
        new("LT", "Litre", "L", null, 1m, 3),
        new("ML", "Mililitre", "ml", "LT", 0.001m, 0),
        new("CL", "Santilitre", "cl", "LT", 0.01m, 1),
        new("M3", "Metreküp", "m³", "LT", 1000m, 3),
        new("GALON", "Galon", "gal", "LT", 3.785412m, 3),

        // Alan (taban: METREKARE)
        new("M2", "Metrekare", "m²", null, 1m, 2),
        new("CM2", "Santimetrekare", "cm²", "M2", 0.0001m, 2),
        new("KM2", "Kilometrekare", "km²", "M2", 1000000m, 4),
        new("HA", "Hektar", "ha", "M2", 10000m, 4),
        new("DA", "Dekar", "da", "M2", 1000m, 3),

        // Zaman (taban: SAAT)
        new("SAAT", "Saat", "sa", null, 1m, 2),
        new("DK", "Dakika", "dk", "SAAT", 0.0166667m, 0),
        new("SN", "Saniye", "sn", "SAAT", 0.000277778m, 0),
        new("GUN", "Gün", "gün", "SAAT", 24m, 2),
    };
}
