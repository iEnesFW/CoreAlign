using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;

/// <summary>
/// Canonicalised projector for the BOM snapshot embedded in <see cref="GlassWorkOrder.BomSnapshotJson"/>.
/// Emits a single deterministic shape (sorted by productId then sortOrder, fixed property order, camelCase,
/// no indentation) for both in-memory drafts and persisted rows, so identical compositions produce byte-identical
/// JSON and idempotent revisions stay stable across recomputes.
/// </summary>
public static class BomSnapshotJsonBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string Build(IEnumerable<BOMLineDraft> lines) =>
        Serialize(lines.Select(l => Project(
            productId: l.ProductId,
            quantity: l.Quantity,
            unitCost: l.UnitCost,
            isService: l.IsService,
            cutSpecJson: null,
            sortOrder: l.SortOrder)));

    public static string Build(IEnumerable<GlassProjectBOMLine> lines) =>
        Serialize(lines.Select(l => Project(
            productId: l.ProductId,
            quantity: l.Quantity,
            unitCost: l.UnitCost,
            isService: l.IsService,
            cutSpecJson: l.CutSpecJson,
            sortOrder: l.SortOrder)));

    /// <summary>
    /// Structural equality on the canonical projection. Use this instead of raw string comparison so semantic
    /// equality survives incidental whitespace, property-order or null vs. missing differences from older snapshots.
    /// </summary>
    public static bool SnapshotsEqual(string? leftJson, string? rightJson)
    {
        if (string.IsNullOrWhiteSpace(leftJson) && string.IsNullOrWhiteSpace(rightJson)) return true;
        if (string.IsNullOrWhiteSpace(leftJson) || string.IsNullOrWhiteSpace(rightJson)) return false;
        try
        {
            using var left = JsonDocument.Parse(leftJson);
            using var right = JsonDocument.Parse(rightJson);
            return JsonElementEquals(left.RootElement, right.RootElement);
        }
        catch (JsonException)
        {
            return string.Equals(leftJson, rightJson, StringComparison.Ordinal);
        }
    }

    private static SnapshotLine Project(
        Guid? productId,
        decimal quantity,
        decimal unitCost,
        bool isService,
        string? cutSpecJson,
        int sortOrder) => new(
            productId,
            quantity,
            unitCost,
            decimal.Round(quantity * unitCost, 4),
            isService,
            cutSpecJson,
            sortOrder);

    private static string Serialize(IEnumerable<SnapshotLine> lines)
    {
        var ordered = lines
            .OrderBy(l => l.ProductId ?? Guid.Empty)
            .ThenBy(l => l.SortOrder)
            .ThenBy(l => l.Quantity)
            .ThenBy(l => l.UnitCost)
            .ToList();
        return JsonSerializer.Serialize(ordered, SerializerOptions);
    }

    private static bool JsonElementEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind) return false;
        switch (a.ValueKind)
        {
            case JsonValueKind.Object:
                var aProps = a.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
                var bProps = b.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
                if (aProps.Count != bProps.Count) return false;
                for (var i = 0; i < aProps.Count; i++)
                {
                    if (!string.Equals(aProps[i].Name, bProps[i].Name, StringComparison.Ordinal)) return false;
                    if (!JsonElementEquals(aProps[i].Value, bProps[i].Value)) return false;
                }
                return true;
            case JsonValueKind.Array:
                var aItems = a.EnumerateArray().ToList();
                var bItems = b.EnumerateArray().ToList();
                if (aItems.Count != bItems.Count) return false;
                for (var i = 0; i < aItems.Count; i++)
                {
                    if (!JsonElementEquals(aItems[i], bItems[i])) return false;
                }
                return true;
            case JsonValueKind.String:
                return string.Equals(a.GetString(), b.GetString(), StringComparison.Ordinal);
            case JsonValueKind.Number:
                return a.GetRawText() == b.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
            default:
                return a.GetRawText() == b.GetRawText();
        }
    }

    private sealed record SnapshotLine(
        [property: JsonPropertyOrder(0)] Guid? ProductId,
        [property: JsonPropertyOrder(1)] decimal Quantity,
        [property: JsonPropertyOrder(2)] decimal UnitCost,
        [property: JsonPropertyOrder(3)] decimal LineTotal,
        [property: JsonPropertyOrder(4)] bool IsService,
        [property: JsonPropertyOrder(5)] string? CutSpecJson,
        [property: JsonPropertyOrder(6)] int SortOrder);
}
