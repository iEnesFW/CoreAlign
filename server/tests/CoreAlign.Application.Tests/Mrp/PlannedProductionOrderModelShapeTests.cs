using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Mrp;

public class PlannedProductionOrderModelShapeTests
{
    [Fact]
    public async Task Model_indexes_match_phase73_migration_exactly()
    {
        await using var fixture = await MrpDbContextFixture.CreateAsync();
        var entity = fixture.Db.Model.FindEntityType(typeof(PlannedProductionOrder));

        entity.Should().NotBeNull();

        var indexedColumnSets = entity!
            .GetIndexes()
            .Select(ix => string.Join(",", ix.Properties.Select(p => p.Name)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        indexedColumnSets.Should().BeEquivalentTo(new[]
        {
            "TenantId,ProductId",
            "TenantId,SourcePlanRunId",
            "TenantId,SourcePlanRunId,PeggingSourceOrderLineId",
        }, "the EF model must match the 3 indexes created by Phase73 — a second "
           + "IEntityTypeConfiguration would silently add a stray index and drift the model from the migration");
    }

    [Fact]
    public async Task Model_index_names_match_phase73_migration_exactly()
    {
        await using var fixture = await MrpDbContextFixture.CreateAsync();
        var entity = fixture.Db.Model.FindEntityType(typeof(PlannedProductionOrder));

        var indexNames = entity!
            .GetIndexes()
            .Select(ix => ix.GetDatabaseName())
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        indexNames.Should().BeEquivalentTo(new[]
        {
            "ix_planned_production_orders_tenant_id_product_id",
            "ix_planned_production_orders_tenant_id_source_plan_run_id",
            "ix_planned_production_orders_tenant_run_pegging_order_line",
        }, "EF-generated index names must equal the CREATE INDEX names in Phase73, "
           + "else Postgres ends up with duplicate physical indexes");
    }

    [Fact]
    public async Task Maps_to_distinct_planned_production_orders_table()
    {
        await using var fixture = await MrpDbContextFixture.CreateAsync();
        var entity = fixture.Db.Model.FindEntityType(typeof(PlannedProductionOrder));

        entity!.GetTableName().Should().Be("planned_production_orders");
    }
}
