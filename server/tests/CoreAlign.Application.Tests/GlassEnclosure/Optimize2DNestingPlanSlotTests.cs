using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.GlassEnclosure.Cutting;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class Optimize2DNestingPlanSlotTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCuttingPlanRepository _plans = new();
    private readonly GlassProject _project;
    private readonly GlassEnclosureSettings _settings;
    private readonly Dictionary<Guid, GlassType> _glassTypes = new();

    public Optimize2DNestingPlanSlotTests()
    {
        _project = new GlassProject("PRJ-NEST", Guid.NewGuid(), "Nesting", Guid.NewGuid()) { TenantId = _tenantId };
        _settings = new GlassEnclosureSettings(_tenantId);

        var glass = new GlassType("CLR6", "Clear 6", 6, GlassStructure.Tempered, 100m, 15m, 1200m, 6m, 1.1m, 32m)
        {
            TenantId = _tenantId,
        };
        _glassTypes[glass.Id] = glass;

        var run = new GlassProjectRun(_project.Id, 0, "R1", 3000, 2100, Guid.NewGuid()) { TenantId = _tenantId };
        run.AddPanel(new GlassProjectPanel(run.Id, 0, 1000, GlassOpeningType.Fixed, glass.Id) { TenantId = _tenantId });
        _project.AddRun(run);
    }

    [Fact]
    public async Task Optimizing_the_nesting_leaves_the_saved_glass_cutting_report_intact()
    {
        await GenerateHandler().Handle(new GenerateCuttingPlanCommand(_project.Id), default);
        var before = await ReportHandler().Handle(new GetCuttingReportQuery(_project.Id), default);

        await OptimizeHandler().Handle(NestingCommand(), default);
        var after = await ReportHandler().Handle(new GetCuttingReportQuery(_project.Id), default);

        after.Should().NotBeNull();
        after!.Glass2D.TotalSheets.Should().Be(1);
        after.Glass2D.SheetWidthMm.Should().Be(_settings.DefaultJumboGlassWidthMm);
        after.Glass2D.Sheets.Should().ContainSingle();
        after.Glass2D.Sheets[0].Placements.Should().ContainSingle();
        after.Glass2D.Should().BeEquivalentTo(before!.Glass2D);
    }

    [Fact]
    public async Task Nesting_is_persisted_in_its_own_slot_next_to_the_cutting_plan()
    {
        await GenerateHandler().Handle(new GenerateCuttingPlanCommand(_project.Id), default);
        await OptimizeHandler().Handle(NestingCommand(), default);

        _plans.Plans.Select(p => p.PlanType).Should().BeEquivalentTo(new[]
        {
            GlassCuttingPlanType.Profile1D,
            GlassCuttingPlanType.Glass2D,
            GlassCuttingPlanType.Glass2DNesting,
        });
    }

    [Fact]
    public async Task A_legacy_nesting_row_left_in_the_glass_slot_is_reported_as_missing_not_as_broken_sheets()
    {
        var nestingReport = await OptimizeHandler().Handle(NestingCommand(), default);
        _plans.Plans.Clear();
        _plans.Plans.Add(new GlassProjectCuttingPlan(
            _project.Id,
            GlassCuttingPlanType.Glass2D,
            System.Text.Json.JsonSerializer.Serialize(nestingReport),
            0m,
            0m,
            0m,
            Guid.NewGuid()));

        var report = await ReportHandler().Handle(new GetCuttingReportQuery(_project.Id), default);

        report.Should().BeNull();
    }

    [Fact]
    public async Task A_valid_report_behind_a_legacy_nesting_row_is_still_recovered()
    {
        // The shape 5 real projects are in: an "optimise" wrote the nesting payload into the glass
        // slot AFTER a good cutting report, so the newest row is the unreadable one. Reading only
        // the newest would throw away work the customer can still use.
        await GenerateHandler().Handle(new GenerateCuttingPlanCommand(_project.Id), default);
        var nestingReport = await OptimizeHandler().Handle(NestingCommand(), default);
        _plans.Plans.Add(new GlassProjectCuttingPlan(
            _project.Id,
            GlassCuttingPlanType.Glass2D,
            System.Text.Json.JsonSerializer.Serialize(nestingReport),
            0m,
            0m,
            0m,
            Guid.NewGuid()));

        var report = await ReportHandler().Handle(new GetCuttingReportQuery(_project.Id), default);

        report.Should().NotBeNull();
        report!.Glass2D.SheetWidthMm.Should().BeGreaterThan(0);
        report.Glass2D.Sheets.Should().NotBeEmpty();
    }

    private Optimize2DNestingCommand NestingCommand() =>
        new(_project.Id, "MaxRects", "BestShortSideFit", MinimizeSheets: true, AcceptableUtilization: 0.85m,
            GuillotineOnly: false, AllowRotation: true);

    private IGlassProjectRepository ProjectRepo()
    {
        var repo = Substitute.For<IGlassProjectRepository>();
        repo.GetByIdWithRunsAsync(_project.Id, Arg.Any<CancellationToken>()).Returns(_project);
        return repo;
    }

    private IGlassEnclosureSettingsRepository SettingsRepo()
    {
        var repo = Substitute.For<IGlassEnclosureSettingsRepository>();
        repo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>()).Returns(_settings);
        return repo;
    }

    private static ICurrentUserAccessor CurrentUser()
    {
        var user = Substitute.For<ICurrentUserAccessor>();
        user.UserId.Returns(Guid.NewGuid());
        return user;
    }

    private GenerateCuttingPlanCommandHandler GenerateHandler()
    {
        var systemRepo = Substitute.For<IProfileSystemRepository>();
        systemRepo.GetWithItemsByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, ProfileSystem>)new Dictionary<Guid, ProfileSystem>());

        var glassRepo = Substitute.For<IGlassTypeRepository>();
        glassRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, GlassType>)_glassTypes);

        return new GenerateCuttingPlanCommandHandler(
            ProjectRepo(),
            systemRepo,
            glassRepo,
            SettingsRepo(),
            new FirstFitDecreasingOptimizer1D(),
            new MaximalRectanglesOptimizer2D(),
            _plans,
            CurrentUser());
    }

    [Fact]
    public async Task Panels_of_different_glass_are_never_nested_on_the_same_sheet()
    {
        // Two panels, identical size, different glass. Keyed by size alone they merged into one
        // request and shared a jumbo — a cut plan that cannot be executed.
        var thick = new GlassType("CLR8", "Clear 8", 8, GlassStructure.Tempered, 130m, 20m, 1200m, 8m, 1.0m, 34m)
        {
            TenantId = _tenantId,
        };
        _glassTypes[thick.Id] = thick;

        var run = new GlassProjectRun(_project.Id, 1, "R2", 3000, 2100, Guid.NewGuid()) { TenantId = _tenantId };
        run.AddPanel(new GlassProjectPanel(run.Id, 0, 1000, GlassOpeningType.Fixed, thick.Id) { TenantId = _tenantId });
        _project.AddRun(run);

        var report = await OptimizeHandler().Handle(NestingCommand(), default);

        report.Sheets.Should().HaveCount(2);
        report.Sheets.Select(s => s.GlassLabel).Should().BeEquivalentTo(new[] { "CLR6 · 6 mm", "CLR8 · 8 mm" });
        report.Sheets.Select(s => s.SheetIndex).Should().BeEquivalentTo(new[] { 1, 2 });
        report.SheetsUsed.Should().Be(2);
        report.Sheets.Sum(s => s.Panels.Count).Should().Be(2);
    }

    [Fact]
    public async Task Same_glass_panels_still_share_one_sheet()
    {
        var run = new GlassProjectRun(_project.Id, 1, "R2", 3000, 2100, Guid.NewGuid()) { TenantId = _tenantId };
        var sameGlassId = _glassTypes.Keys.First();
        run.AddPanel(new GlassProjectPanel(run.Id, 0, 1000, GlassOpeningType.Fixed, sameGlassId) { TenantId = _tenantId });
        _project.AddRun(run);

        var report = await OptimizeHandler().Handle(NestingCommand(), default);

        report.Sheets.Should().ContainSingle();
        report.Sheets[0].Panels.Should().HaveCount(2);
    }

    private IGlassTypeRepository GlassRepo()
    {
        var repo = Substitute.For<IGlassTypeRepository>();
        repo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, GlassType>)_glassTypes);
        return repo;
    }

    private Optimize2DNestingCommandHandler OptimizeHandler() =>
        new(ProjectRepo(), SettingsRepo(), new MaxRectsGlass2DOptimizer(), _plans, GlassRepo(), CurrentUser());

    private GetCuttingReportQueryHandler ReportHandler() => new(_plans);
}
