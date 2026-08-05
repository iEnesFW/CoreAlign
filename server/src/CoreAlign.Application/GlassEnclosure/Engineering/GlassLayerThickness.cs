using System.Text.Json;

namespace CoreAlign.Application.GlassEnclosure.Engineering;

/// <summary>
/// The glass plies of a build-up, read out of <c>GlassType.GlassLayersJson</c>.
///
/// The column is free-form catalogue data, so this reads what it can and returns nothing rather
/// than throwing: a laminate whose layers were never entered simply falls back to the monolithic
/// check on the nominal thickness, which is what the calculator did for every pane before.
/// Interlayers (PVB, SGP) carry no bending stress and are skipped.
/// </summary>
public static class GlassLayerThickness
{
    private static readonly string[] ThicknessKeys =
    {
        "thicknessMm", "thickness", "tMm", "t",
    };

    private static readonly string[] InterlayerMarkers =
    {
        "pvb", "sgp", "eva", "interlayer", "ara katman", "film",
    };

    public static IReadOnlyList<decimal> Parse(string? layersJson)
    {
        if (string.IsNullOrWhiteSpace(layersJson)) return Array.Empty<decimal>();
        try
        {
            using var doc = JsonDocument.Parse(layersJson, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
            });
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return Array.Empty<decimal>();

            var plies = new List<decimal>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Number
                    && element.TryGetDecimal(out var bare)
                    && bare > 0m)
                {
                    plies.Add(bare);
                    continue;
                }
                if (element.ValueKind != JsonValueKind.Object) continue;
                if (IsInterlayer(element)) continue;

                foreach (var key in ThicknessKeys)
                {
                    if (!element.TryGetProperty(key, out var value)) continue;
                    if (value.ValueKind == JsonValueKind.Number
                        && value.TryGetDecimal(out var t)
                        && t > 0m)
                    {
                        plies.Add(t);
                    }
                    break;
                }
            }
            return plies;
        }
        catch (JsonException)
        {
            return Array.Empty<decimal>();
        }
    }

    private static bool IsInterlayer(JsonElement element)
    {
        foreach (var name in new[] { "kind", "type", "material", "name" })
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind != JsonValueKind.String) continue;
            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text)) continue;
            foreach (var marker in InterlayerMarkers)
            {
                if (text.Contains(marker, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }

}
