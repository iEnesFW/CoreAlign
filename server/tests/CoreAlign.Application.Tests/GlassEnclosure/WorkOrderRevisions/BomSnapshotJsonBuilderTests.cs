using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure.WorkOrderRevisions;

public class BomSnapshotJsonBuilderTests
{
    [Fact]
    public void Build_from_BOMLineResult_and_GlassProjectBOMLine_produces_identical_structure()
    {
        var productId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var refId = Guid.NewGuid();

        var draft = new BOMLineDraft(
            Kind: GlassBOMLineKind.ProfileCut,
            RefId: refId,
            ProductId: productId,
            IsService: false,
            Description: "Top profile",
            Quantity: 2.5m,
            Unit: "m",
            UnitCost: 40.1234m,
            Currency: "TRY",
            Source: "run-1",
            SortOrder: 0);

        var entity = new GlassProjectBOMLine(
            projectId: projectId,
            kind: GlassBOMLineKind.ProfileCut,
            description: "Top profile",
            quantity: 2.5m,
            unit: "m",
            unitCost: 40.1234m,
            currency: "TRY",
            refId: refId,
            source: "run-1",
            sortOrder: 0,
            productId: productId,
            isService: false,
            cutSpecJson: null);

        var draftJson = BomSnapshotJsonBuilder.Build(new[] { draft });
        var entityJson = BomSnapshotJsonBuilder.Build(new[] { entity });

        draftJson.Should().Be(entityJson);
    }

    [Fact]
    public void Build_emits_stable_property_names_for_round_trip_diffs()
    {
        var entity = new GlassProjectBOMLine(
            projectId: Guid.NewGuid(),
            kind: GlassBOMLineKind.GlassPiece,
            description: "Panel",
            quantity: 1m,
            unit: "m²",
            unitCost: 100m,
            currency: "TRY",
            productId: Guid.NewGuid(),
            cutSpecJson: null);

        var json = BomSnapshotJsonBuilder.Build(new[] { entity });

        json.Should().Contain("\"productId\"");
        json.Should().Contain("\"quantity\"");
        json.Should().Contain("\"unitCost\"");
        json.Should().Contain("\"lineTotal\"");
        json.Should().Contain("\"isService\"");
        json.Should().Contain("\"cutSpecJson\"");
    }
}
