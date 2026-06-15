using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.HostedServices;

public static class ProjectTemplateSeeder
{
    public static async Task SeedSystemTemplatesAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<IProjectTemplateRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var spec in SystemTemplates)
        {
            var existing = await repo.GetByCodeAsync(spec.Code, ct);
            if (existing is not null) continue;

            var template = new ProjectTemplate(
                code: spec.Code,
                displayNameKey: spec.DisplayNameKey,
                isSystemTemplate: true,
                category: spec.Category,
                subtype: spec.Subtype,
                geometryMode: spec.GeometryMode,
                mountingTopology: spec.MountingTopology,
                defaultConnectorKind: spec.DefaultConnectorKind,
                roofPitchDeg: spec.RoofPitchDeg,
                ridgeHeightMm: null,
                eaveHeightMm: null,
                thumbnailUrl: null,
                descriptionKey: spec.DescriptionKey,
                metadataJson: null,
                sortOrder: spec.SortOrder)
            {
                TenantId = Guid.Empty,
            };

            foreach (var presetSpec in spec.RunPresets)
            {
                template.AddRunPreset(new ProjectTemplateRunPreset(
                    templateId: template.Id,
                    orderIndex: presetSpec.OrderIndex,
                    labelKey: presetSpec.LabelKey,
                    lengthMm: presetSpec.LengthMm,
                    heightMm: presetSpec.HeightMm,
                    defaultPanelCount: presetSpec.DefaultPanelCount,
                    defaultPanelWidthMm: presetSpec.DefaultPanelWidthMm,
                    defaultOpeningType: presetSpec.DefaultOpeningType,
                    originX: presetSpec.OriginX,
                    originY: presetSpec.OriginY,
                    rotationDeg: presetSpec.RotationDeg,
                    hasTopDrip: presetSpec.HasTopDrip,
                    hasBottomThreshold: presetSpec.HasBottomThreshold,
                    connectsToPreviousAsCorner: presetSpec.ConnectsToPreviousAsCorner,
                    cornerJointAngleDeg: presetSpec.CornerJointAngleDeg,
                    cornerUsesPost: presetSpec.CornerUsesPost)
                {
                    TenantId = Guid.Empty,
                });
            }

            await repo.AddAsync(template, ct);
            anyChange = true;
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private record TemplateSpec(
        string Code,
        string DisplayNameKey,
        string DescriptionKey,
        EnclosureCategory Category,
        EnclosureSubtype Subtype,
        GeometryMode GeometryMode,
        MountingTopology MountingTopology,
        ConnectorKind DefaultConnectorKind,
        decimal? RoofPitchDeg,
        int SortOrder,
        RunPresetSpec[] RunPresets);

    private record RunPresetSpec(
        int OrderIndex,
        string LabelKey,
        int LengthMm,
        int HeightMm,
        int DefaultPanelCount,
        int DefaultPanelWidthMm,
        GlassOpeningType DefaultOpeningType,
        decimal OriginX = 0m,
        decimal OriginY = 0m,
        decimal RotationDeg = 0m,
        bool HasTopDrip = false,
        bool HasBottomThreshold = false,
        bool ConnectsToPreviousAsCorner = false,
        decimal? CornerJointAngleDeg = null,
        bool CornerUsesPost = false);

    private static readonly TemplateSpec[] SystemTemplates =
    {
        new(
            Code: "SYS-L-BALCONY",
            DisplayNameKey: "GlassEnclosure.Template.LBalcony",
            DescriptionKey: "GlassEnclosure.Template.LBalcony.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.CornerProfile,
            RoofPitchDeg: null,
            SortOrder: 10,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Front", 3000, 2400, 4, 750, GlassOpeningType.SlidingLeft),
                new RunPresetSpec(1, "GlassEnclosure.Run.Side", 2000, 2400, 3, 666, GlassOpeningType.SlidingLeft,
                    RotationDeg: 90m, ConnectsToPreviousAsCorner: true, CornerJointAngleDeg: 90m, CornerUsesPost: true),
                new RunPresetSpec(2, "GlassEnclosure.Run.Back", 3000, 2400, 4, 750, GlassOpeningType.Fixed,
                    RotationDeg: 180m),
            }),
        new(
            Code: "SYS-U-BALCONY",
            DisplayNameKey: "GlassEnclosure.Template.UBalcony",
            DescriptionKey: "GlassEnclosure.Template.UBalcony.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.CornerProfile,
            RoofPitchDeg: null,
            SortOrder: 20,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.LeftWing", 1800, 2400, 3, 600, GlassOpeningType.SlidingLeft),
                new RunPresetSpec(1, "GlassEnclosure.Run.Front", 3600, 2400, 4, 900, GlassOpeningType.SlidingLeft,
                    RotationDeg: 90m, ConnectsToPreviousAsCorner: true, CornerJointAngleDeg: 90m, CornerUsesPost: true),
                new RunPresetSpec(2, "GlassEnclosure.Run.RightWing", 1800, 2400, 3, 600, GlassOpeningType.SlidingRight,
                    RotationDeg: 180m, ConnectsToPreviousAsCorner: true, CornerJointAngleDeg: 90m, CornerUsesPost: true),
            }),
        new(
            Code: "SYS-FLAT-TERRACE",
            DisplayNameKey: "GlassEnclosure.Template.FlatTerrace",
            DescriptionKey: "GlassEnclosure.Template.FlatTerrace.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.Profile,
            RoofPitchDeg: null,
            SortOrder: 30,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Front", 3000, 2400, 4, 750, GlassOpeningType.SlidingLeft,
                    HasTopDrip: true),
            }),
        new(
            Code: "SYS-CORNER-LOGGIA",
            DisplayNameKey: "GlassEnclosure.Template.CornerLoggia",
            DescriptionKey: "GlassEnclosure.Template.CornerLoggia.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.CornerProfile,
            RoofPitchDeg: null,
            SortOrder: 40,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Front", 2400, 2400, 3, 800, GlassOpeningType.SlidingLeft),
                new RunPresetSpec(1, "GlassEnclosure.Run.Side", 1800, 2400, 3, 600, GlassOpeningType.SlidingRight,
                    RotationDeg: 90m, ConnectsToPreviousAsCorner: true, CornerJointAngleDeg: 90m, CornerUsesPost: true),
            }),
        new(
            Code: "SYS-GUILLOTINE-SINGLE",
            DisplayNameKey: "GlassEnclosure.Template.GuillotineSingle",
            DescriptionKey: "GlassEnclosure.Template.GuillotineSingle.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.Profile,
            RoofPitchDeg: null,
            SortOrder: 50,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.GuillotineSash", 1200, 2400, 1, 1200, GlassOpeningType.Fixed),
            }),
        new(
            Code: "SYS-HEATINS-SLIDING-3",
            DisplayNameKey: "GlassEnclosure.Template.HeatInsSliding3",
            DescriptionKey: "GlassEnclosure.Template.HeatInsSliding3.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.Profile,
            RoofPitchDeg: null,
            SortOrder: 60,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Front", 3000, 2400, 3, 1000, GlassOpeningType.SlidingLeft,
                    HasBottomThreshold: true),
            }),
        new(
            Code: "SYS-GREENHOUSE-FLAT",
            DisplayNameKey: "GlassEnclosure.Template.GreenhouseFlat",
            DescriptionKey: "GlassEnclosure.Template.GreenhouseFlat.Description",
            Category: EnclosureCategory.HorizontalOrPitched,
            Subtype: EnclosureSubtype.Greenhouse,
            GeometryMode: GeometryMode.Pitched,
            MountingTopology: MountingTopology.RoofAnchored,
            DefaultConnectorKind: ConnectorKind.Profile,
            RoofPitchDeg: 15m,
            SortOrder: 70,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Roof", 4000, 2000, 4, 1000, GlassOpeningType.Fixed,
                    HasTopDrip: true),
            }),
        new(
            Code: "SYS-BALUSTRADE-CORRIDOR",
            DisplayNameKey: "GlassEnclosure.Template.BalustradeCorridor",
            DescriptionKey: "GlassEnclosure.Template.BalustradeCorridor.Description",
            Category: EnclosureCategory.Functional,
            Subtype: EnclosureSubtype.Balustrade,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ChannelBase,
            DefaultConnectorKind: ConnectorKind.UChannel,
            RoofPitchDeg: null,
            SortOrder: 80,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Corridor", 6000, 1100, 6, 1000, GlassOpeningType.Fixed),
            }),
        new(
            Code: "SYS-SHOWER-CORNER",
            DisplayNameKey: "GlassEnclosure.Template.ShowerCorner",
            DescriptionKey: "GlassEnclosure.Template.ShowerCorner.Description",
            Category: EnclosureCategory.Functional,
            Subtype: EnclosureSubtype.ShowerCabin,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.WallAnchored,
            DefaultConnectorKind: ConnectorKind.GlassToGlassPolish,
            RoofPitchDeg: null,
            SortOrder: 90,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.ShowerFront", 900, 2000, 1, 900, GlassOpeningType.Fixed),
                new RunPresetSpec(1, "GlassEnclosure.Run.ShowerSide", 900, 2000, 1, 900, GlassOpeningType.Fixed,
                    RotationDeg: 90m, ConnectsToPreviousAsCorner: true, CornerJointAngleDeg: 90m, CornerUsesPost: false),
            }),
        new(
            Code: "SYS-OFFICE-PARTITION-L",
            DisplayNameKey: "GlassEnclosure.Template.OfficePartitionL",
            DescriptionKey: "GlassEnclosure.Template.OfficePartitionL.Description",
            Category: EnclosureCategory.Functional,
            Subtype: EnclosureSubtype.OfficePartition,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.FloorAnchored,
            DefaultConnectorKind: ConnectorKind.HShapeProfile,
            RoofPitchDeg: null,
            SortOrder: 100,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Front", 3000, 2700, 3, 1000, GlassOpeningType.Fixed),
                new RunPresetSpec(1, "GlassEnclosure.Run.Side", 2400, 2700, 3, 800, GlassOpeningType.Fixed,
                    RotationDeg: 90m, ConnectsToPreviousAsCorner: true, CornerJointAngleDeg: 90m, CornerUsesPost: false),
            }),
        new(
            Code: "SYS-OFFICE-CURTAINWALL",
            DisplayNameKey: "GlassEnclosure.Template.OfficeCurtainWall",
            DescriptionKey: "GlassEnclosure.Template.OfficeCurtainWall.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.CurtainWall,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            DefaultConnectorKind: ConnectorKind.Profile,
            RoofPitchDeg: null,
            SortOrder: 110,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.Facade", 8000, 3000, 16, 500, GlassOpeningType.Fixed),
            }),
        new(
            Code: "SYS-LOBBY-SPIDERFACADE",
            DisplayNameKey: "GlassEnclosure.Template.LobbySpiderFacade",
            DescriptionKey: "GlassEnclosure.Template.LobbySpiderFacade.Description",
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.SpiderFacade,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.SpiderArm,
            DefaultConnectorKind: ConnectorKind.SpiderFitting,
            RoofPitchDeg: null,
            SortOrder: 120,
            RunPresets: new[]
            {
                new RunPresetSpec(0, "GlassEnclosure.Run.LobbyFront", 6000, 4000, 4, 1500, GlassOpeningType.Fixed),
            }),
        new(
            Code: "SYS-FREEFORM-BLANK",
            DisplayNameKey: "GlassEnclosure.Template.FreeFormBlank",
            DescriptionKey: "GlassEnclosure.Template.FreeFormBlank.Description",
            Category: EnclosureCategory.Special,
            Subtype: EnclosureSubtype.FreeForm,
            GeometryMode: GeometryMode.FreeForm,
            MountingTopology: MountingTopology.SelfSupporting,
            DefaultConnectorKind: ConnectorKind.StructuralSilicone,
            RoofPitchDeg: null,
            SortOrder: 130,
            RunPresets: Array.Empty<RunPresetSpec>()),
    };
}
