using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class SceneValidatorBendRadiusTests
{
    private readonly IProfileSystemRepository _systemRepo = Substitute.For<IProfileSystemRepository>();
    private readonly IGlassTypeRepository _glassRepo = Substitute.For<IGlassTypeRepository>();
    private readonly IWindZoneRepository _windRepo = Substitute.For<IWindZoneRepository>();
    private readonly IClimateZoneRepository _climateRepo = Substitute.For<IClimateZoneRepository>();

    private SceneValidator CreateSut() => new(_systemRepo, _glassRepo, _windRepo, _climateRepo);

    private GlassProject BuildProject(int? arcRadiusMm, decimal? arcSweepDeg, bool arcGlassBent, int glassThicknessMm)
    {
        var system = new ProfileSystem(
            "SYS", "System", Guid.NewGuid(), GlassSystemType.Sliding,
            maxPanelWidthMm: 5000, maxPanelHeightMm: 4000, maxPanelWeightKg: 500m,
            supportedGlassThicknessesJson: $"[{glassThicknessMm}]",
            supportedOpeningsJson: "[\"Fixed\"]");
        var glass = new GlassType(
            "GT", "Glass", glassThicknessMm, GlassStructure.Tempered,
            pricePerM2: 100m, weightKgPerM2: 20m, allowablePressurePa: 0m,
            maxPanelAreaM2: 0m, uValue: 1m, soundDb: 30m);
        _systemRepo.GetByIdAsync(system.Id, Arg.Any<CancellationToken>()).Returns(system);
        _glassRepo.GetByIdAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(glass);

        var project = new GlassProject("GP-1", Guid.NewGuid(), "Project", Guid.NewGuid());
        var run = new GlassProjectRun(project.Id, 0, "Run 1", 2000, 2400, system.Id);
        run.UpdateGeometry3D(null, null, arcRadiusMm, arcSweepDeg, arcGlassBent);
        run.AddPanel(new GlassProjectPanel(run.Id, 0, 1000, GlassOpeningType.Fixed, glass.Id));
        project.AddRun(run);
        return project;
    }

    [Fact]
    public async Task Validating_bent_arc_run_below_min_bend_radius_adds_warning()
    {
        var project = BuildProject(arcRadiusMm: 900, arcSweepDeg: -90m, arcGlassBent: true, glassThicknessMm: 8);

        var result = await CreateSut().ValidateAsync(project);

        var finding = result.Findings.Should().ContainSingle(f => f.Code == "GE.Arc.BendRadiusTight").Subject;
        finding.Severity.Should().Be(GlassValidationSeverity.Warning);
        finding.MessageArgs.Should().Be("900|1200");
    }

    [Fact]
    public async Task Validating_bent_arc_run_at_or_above_min_bend_radius_adds_no_warning()
    {
        var project = BuildProject(arcRadiusMm: 1200, arcSweepDeg: -90m, arcGlassBent: true, glassThicknessMm: 8);

        var result = await CreateSut().ValidateAsync(project);

        result.Findings.Should().NotContain(f => f.Code == "GE.Arc.BendRadiusTight");
    }

    [Fact]
    public async Task Validating_faceted_arc_run_skips_bend_radius_check()
    {
        var project = BuildProject(arcRadiusMm: 500, arcSweepDeg: -90m, arcGlassBent: false, glassThicknessMm: 8);

        var result = await CreateSut().ValidateAsync(project);

        result.Findings.Should().NotContain(f => f.Code == "GE.Arc.BendRadiusTight");
    }

    [Fact]
    public async Task Validating_half_arc_without_sweep_skips_bend_radius_check()
    {
        var project = BuildProject(arcRadiusMm: 500, arcSweepDeg: null, arcGlassBent: true, glassThicknessMm: 8);

        var result = await CreateSut().ValidateAsync(project);

        result.Findings.Should().NotContain(f => f.Code == "GE.Arc.BendRadiusTight");
    }
}
