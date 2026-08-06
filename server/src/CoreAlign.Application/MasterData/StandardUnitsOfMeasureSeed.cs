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
        new("DUZINE", "Düzine", "dz", "ADET", 12m, 0),
        new("CIFT", "Çift", "çift", "ADET", 2m, 0),
        new("PAKET", "Paket", "pk", null, 1m, 0),
        new("KUTU", "Kutu", "kt", null, 1m, 0),
        new("KOLI", "Koli", "koli", null, 1m, 0),
        new("PALET", "Palet", "plt", null, 1m, 0),
        new("TAKIM", "Takım", "tk", null, 1m, 0),
        new("ROLE", "Rulo", "rl", null, 1m, 0),

        // Uzunluk (taban: METRE)
        new("METRE", "Metre", "m", null, 1m, 2),
        new("MILIMETRE", "Milimetre", "mm", "METRE", 0.001m, 0),
        new("SANTIMETRE", "Santimetre", "cm", "METRE", 0.01m, 1),
        new("DESIMETRE", "Desimetre", "dm", "METRE", 0.1m, 2),
        new("KILOMETRE", "Kilometre", "km", "METRE", 1000m, 3),
        new("INC", "İnç", "in", "METRE", 0.0254m, 2),
        new("FIT", "Fit", "ft", "METRE", 0.3048m, 2),
        new("YARDA", "Yarda", "yd", "METRE", 0.9144m, 2),

        // Ağırlık (taban: KILOGRAM)
        new("KILOGRAM", "Kilogram", "kg", null, 1m, 3),
        new("GRAM", "Gram", "g", "KILOGRAM", 0.001m, 1),
        new("MILIGRAM", "Miligram", "mg", "KILOGRAM", 0.000001m, 0),
        new("TON", "Ton", "t", "KILOGRAM", 1000m, 3),
        new("LIBRE", "Libre", "lb", "KILOGRAM", 0.453592m, 3),
        new("ONS", "Ons", "oz", "KILOGRAM", 0.0283495m, 3),

        // Hacim (taban: LITRE)
        new("LITRE", "Litre", "L", null, 1m, 3),
        new("MILILITRE", "Mililitre", "ml", "LITRE", 0.001m, 0),
        new("SANTILITRE", "Santilitre", "cl", "LITRE", 0.01m, 1),
        new("METREKUP", "Metreküp", "m³", "LITRE", 1000m, 3),
        new("GALON", "Galon", "gal", "LITRE", 3.785412m, 3),

        // Alan (taban: METREKARE)
        new("METREKARE", "Metrekare", "m²", null, 1m, 2),
        new("SANTIMETREKARE", "Santimetrekare", "cm²", "METREKARE", 0.0001m, 2),
        new("KILOMETREKARE", "Kilometrekare", "km²", "METREKARE", 1000000m, 4),
        new("HEKTAR", "Hektar", "ha", "METREKARE", 10000m, 4),
        new("DEKAR", "Dekar", "da", "METREKARE", 1000m, 3),

        // Zaman (taban: SAAT)
        new("SAAT", "Saat", "sa", null, 1m, 2),
        new("DAKIKA", "Dakika", "dk", "SAAT", 0.0166667m, 0),
        new("SANIYE", "Saniye", "sn", "SAAT", 0.000277778m, 0),
        new("GUN", "Gün", "gün", "SAAT", 24m, 2),
    };
}
