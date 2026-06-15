using System.Text.Json;

namespace CoreAlign.Application.Installation.Templates;

public static class StandardChecklist
{
    public static readonly IReadOnlyList<ChecklistCategory> GlassEnclosureChecklist = new[]
    {
        new ChecklistCategory("Mechanical", new[]
        {
            new ChecklistItem("Mechanical.FrameRigidity"),
            new ChecklistItem("Mechanical.AnchorBolts"),
            new ChecklistItem("Mechanical.Levelness"),
            new ChecklistItem("Mechanical.ScrewFastenings"),
        }),
        new ChecklistCategory("Glass", new[]
        {
            new ChecklistItem("Glass.NoChips"),
            new ChecklistItem("Glass.PanelAlignment"),
            new ChecklistItem("Glass.EdgePolish"),
            new ChecklistItem("Glass.StampVisible"),
        }),
        new ChecklistCategory("Seal", new[]
        {
            new ChecklistItem("Seal.SiliconeApplied"),
            new ChecklistItem("Seal.NoGaps"),
            new ChecklistItem("Seal.WaterTest"),
        }),
        new ChecklistCategory("Dimension", new[]
        {
            new ChecklistItem("Dimension.WidthTolerance"),
            new ChecklistItem("Dimension.HeightTolerance"),
            new ChecklistItem("Dimension.Diagonal"),
        }),
        new ChecklistCategory("Hardware", new[]
        {
            new ChecklistItem("Hardware.HandlesOperational"),
            new ChecklistItem("Hardware.LocksOperational"),
            new ChecklistItem("Hardware.HingesLubricated"),
            new ChecklistItem("Hardware.HingeAlignment"),
        }),
    };

    public static string BuildInitialChecklistJson()
    {
        var snapshot = GlassEnclosureChecklist.Select(c => new
        {
            category = c.Key,
            items = c.Items.Select(i => new
            {
                key = i.Key,
                result = "NotEvaluated",
                notes = (string?)null,
            }).ToArray()
        }).ToArray();
        return JsonSerializer.Serialize(snapshot);
    }
}

public sealed record ChecklistCategory(string Key, IReadOnlyList<ChecklistItem> Items);
public sealed record ChecklistItem(string Key);
