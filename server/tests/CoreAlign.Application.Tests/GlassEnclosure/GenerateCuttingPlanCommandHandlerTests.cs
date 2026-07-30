using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class GenerateCuttingPlanCommandHandlerTests
{
    private const int JumboWidthMm = 3210;
    private const int JumboHeightMm = 2250;

    [Fact]
    public async Task Panels_of_different_glass_thickness_are_never_planned_on_the_same_jumbo_sheet()
    {
        var scenario = Scenario.WithTwoGlassTypes(panelWidthMm: 1000, runHeightMm: 2100);

        var report = await scenario.Handler().Handle(new GenerateCuttingPlanCommand(scenario.ProjectId), default);

        var glass = report.Glass2D;
        glass.TotalSheets.Should().Be(2);
        glass.Sheets.Should().OnlyContain(s => s.Placements.Count == 1);
        glass.Sheets.Select(s => s.GroupKey).Should().BeEquivalentTo(new[] { "CLR6 · 6 mm", "CLR8 · 8 mm" });
        glass.Groups.Should().HaveCount(2);
        glass.Groups.Should().OnlyContain(g => g.TotalSheets == 1);
    }

    [Fact]
    public async Task Group_totals_stay_consistent_with_the_report_totals()
    {
        var scenario = Scenario.WithTwoGlassTypes(panelWidthMm: 1000, runHeightMm: 2100);

        var glass = (await scenario.Handler().Handle(new GenerateCuttingPlanCommand(scenario.ProjectId), default)).Glass2D;

        glass.Groups.Sum(g => g.TotalSheets).Should().Be(glass.TotalSheets);
        glass.Groups.Sum(g => g.TotalUsedMm2).Should().Be(glass.TotalUsedMm2);
        glass.Groups.Sum(g => g.TotalWasteMm2).Should().Be(glass.TotalWasteMm2);
        (glass.TotalUsedMm2 + glass.TotalWasteMm2)
            .Should().Be((long)glass.TotalSheets * JumboWidthMm * JumboHeightMm);
    }

    [Fact]
    public async Task Panel_wider_than_the_jumbo_sheet_is_reported_as_a_user_error_naming_the_panel()
    {
        var scenario = Scenario.WithTwoGlassTypes(panelWidthMm: 4000, runHeightMm: 2100);

        var act = async () => await scenario.Handler().Handle(new GenerateCuttingPlanCommand(scenario.ProjectId), default);

        var thrown = (await act.Should().ThrowAsync<GlassCutExceedsJumboSheetException>()).Which;
        thrown.Should().BeAssignableTo<ConflictException>();
        thrown.Message.Should().Contain("4000x2100").And.Contain("3210x2250");
    }

    [Fact]
    public async Task A_rail_longer_than_the_stock_bar_reports_the_splice_pieces_it_was_cut_into()
    {
        var scenario = Scenario.WithLongRail(runLengthMm: 9000, runHeightMm: 2100);

        var profile = (await scenario.Handler().Handle(new GenerateCuttingPlanCommand(scenario.ProjectId), default)).Profile1D;

        var railCuts = profile.Patterns.SelectMany(p => p.Cuts).Where(c => c.Label == "TOP-RAIL").ToList();
        railCuts.Should().HaveCount(2);
        railCuts.Should().OnlyContain(c => c.PieceCount == 2 && c.LengthMm == 4500);
        railCuts.Select(c => c.PieceIndex).OrderBy(i => i).Should().Equal(1, 2);
    }

    [Fact]
    public async Task A_rail_that_fits_a_single_bar_stays_a_one_piece_cut()
    {
        var scenario = Scenario.WithLongRail(runLengthMm: 3000, runHeightMm: 2100);

        var profile = (await scenario.Handler().Handle(new GenerateCuttingPlanCommand(scenario.ProjectId), default)).Profile1D;

        profile.Patterns.SelectMany(p => p.Cuts).Should().OnlyContain(c => c.PieceCount == 1 && c.PieceIndex == 1);
    }

    private sealed class Scenario
    {
        private readonly GlassProject _project;
        private readonly Dictionary<Guid, GlassType> _glassTypes = new();
        private readonly Dictionary<Guid, ProfileSystem> _systems = new();
        private readonly GlassEnclosureSettings _settings;

        public Guid ProjectId => _project.Id;
        public FakeCuttingPlanRepository Plans { get; } = new();

        private Scenario(Guid tenantId)
        {
            _project = new GlassProject("PRJ-CUT", Guid.NewGuid(), "Cutting", Guid.NewGuid()) { TenantId = tenantId };
            _settings = new GlassEnclosureSettings(tenantId);
        }

        public static Scenario WithTwoGlassTypes(int panelWidthMm, int runHeightMm)
        {
            var tenantId = Guid.NewGuid();
            var scenario = new Scenario(tenantId);
            var thin = scenario.AddGlassType(tenantId, "CLR6", 6);
            var thick = scenario.AddGlassType(tenantId, "CLR8", 8);

            var run = new GlassProjectRun(scenario.ProjectId, 0, "R1", 3000, runHeightMm, Guid.NewGuid()) { TenantId = tenantId };
            run.AddPanel(new GlassProjectPanel(run.Id, 0, panelWidthMm, GlassOpeningType.Fixed, thin) { TenantId = tenantId });
            run.AddPanel(new GlassProjectPanel(run.Id, 1, panelWidthMm, GlassOpeningType.Fixed, thick) { TenantId = tenantId });
            scenario._project.AddRun(run);
            return scenario;
        }

        public static Scenario WithLongRail(int runLengthMm, int runHeightMm)
        {
            var tenantId = Guid.NewGuid();
            var scenario = new Scenario(tenantId);
            var glassTypeId = scenario.AddGlassType(tenantId, "CLR6", 6);

            var system = new ProfileSystem(
                "SYS-1", "System", Guid.NewGuid(), GlassSystemType.Sliding, 1200, 3000, 120m, "[]", "[]")
            {
                TenantId = tenantId,
            };
            // WHY: a role with no profile falls back to the first item, which would relabel every
            // segment "TOP-RAIL" and make the splice assertion meaningless.
            system.AddItem(new ProfileItem(system.Id, ProfileRole.Top, "TOP-RAIL", "Top rail", 6000, 1.5m, 10m) { TenantId = tenantId });
            system.AddItem(new ProfileItem(system.Id, ProfileRole.Bottom, "BOT-RAIL", "Bottom rail", 6000, 1.5m, 10m) { TenantId = tenantId });
            system.AddItem(new ProfileItem(system.Id, ProfileRole.SideJamb, "JAMB", "Side jamb", 6000, 1.2m, 10m) { TenantId = tenantId });
            system.AddItem(new ProfileItem(system.Id, ProfileRole.Sash, "SASH", "Sash", 6000, 1.1m, 10m) { TenantId = tenantId });
            scenario._systems[system.Id] = system;

            var run = new GlassProjectRun(scenario.ProjectId, 0, "R1", runLengthMm, runHeightMm, system.Id) { TenantId = tenantId };
            run.AddPanel(new GlassProjectPanel(run.Id, 0, 1000, GlassOpeningType.Fixed, glassTypeId) { TenantId = tenantId });
            scenario._project.AddRun(run);
            return scenario;
        }

        private Guid AddGlassType(Guid tenantId, string code, int thicknessMm)
        {
            var glass = new GlassType(code, code, thicknessMm, GlassStructure.Tempered, 100m, 15m, 1200m, 6m, 1.1m, 32m)
            {
                TenantId = tenantId,
            };
            _glassTypes[glass.Id] = glass;
            return glass.Id;
        }

        public GenerateCuttingPlanCommandHandler Handler()
        {
            var projectRepo = Substitute.For<IGlassProjectRepository>();
            projectRepo.GetByIdWithRunsAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(_project);

            var systemRepo = Substitute.For<IProfileSystemRepository>();
            systemRepo.GetWithItemsByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyDictionary<Guid, ProfileSystem>)_systems);

            var glassRepo = Substitute.For<IGlassTypeRepository>();
            glassRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyDictionary<Guid, GlassType>)_glassTypes);

            var settingsRepo = Substitute.For<IGlassEnclosureSettingsRepository>();
            settingsRepo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>()).Returns(_settings);

            var currentUser = Substitute.For<ICurrentUserAccessor>();
            currentUser.UserId.Returns(Guid.NewGuid());

            return new GenerateCuttingPlanCommandHandler(
                projectRepo,
                systemRepo,
                glassRepo,
                settingsRepo,
                new FirstFitDecreasingOptimizer1D(),
                new MaximalRectanglesOptimizer2D(),
                Plans,
                currentUser);
        }
    }

}
