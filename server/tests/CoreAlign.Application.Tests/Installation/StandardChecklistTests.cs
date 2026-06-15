using System.Text.Json;
using CoreAlign.Application.Installation.Templates;

namespace CoreAlign.Application.Tests.Installation;

public class StandardChecklistTests
{
    [Fact]
    public void GlassEnclosureChecklist_has_five_categories()
    {
        StandardChecklist.GlassEnclosureChecklist.Should().HaveCount(5);
        StandardChecklist.GlassEnclosureChecklist
            .Select(c => c.Key)
            .Should().BeEquivalentTo(new[] { "Mechanical", "Glass", "Seal", "Dimension", "Hardware" });
    }

    [Fact]
    public void GlassEnclosureChecklist_total_item_count_is_at_least_eighteen()
    {
        var total = StandardChecklist.GlassEnclosureChecklist.Sum(c => c.Items.Count);
        total.Should().BeGreaterThanOrEqualTo(18);
    }

    [Fact]
    public void BuildInitialChecklistJson_emits_all_categories_with_not_evaluated_default()
    {
        var json = StandardChecklist.BuildInitialChecklistJson();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(5);

        foreach (var category in doc.RootElement.EnumerateArray())
        {
            category.GetProperty("category").GetString().Should().NotBeNullOrEmpty();
            var items = category.GetProperty("items");
            items.GetArrayLength().Should().BeGreaterThan(0);
            foreach (var item in items.EnumerateArray())
            {
                item.GetProperty("result").GetString().Should().Be("NotEvaluated");
                item.GetProperty("key").GetString().Should().NotBeNullOrEmpty();
                item.GetProperty("label").GetString().Should().NotBeNullOrEmpty();
            }
        }
    }
}
