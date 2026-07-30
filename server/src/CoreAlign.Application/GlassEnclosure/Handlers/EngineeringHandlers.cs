using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class RecomputeBOMCommandHandler : IRequestHandler<RecomputeBOMCommand, BOMSummaryDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IBOMComposer _composer;
    private readonly IGlassProjectBOMLineRepository _lineRepo;
    private readonly IStockAvailabilityService _availabilityService;
    private readonly IBomStaleSignal _bomStaleSignal;
    private readonly IGlassWorkOrderRepository _workOrderRepo;
    private readonly IWorkOrderRevisionService _revisionService;
    private readonly IBomRecomputedOutbox? _bomRecomputedOutbox;

    public RecomputeBOMCommandHandler(
        IGlassProjectRepository projectRepo,
        IBOMComposer composer,
        IGlassProjectBOMLineRepository lineRepo,
        IStockAvailabilityService availabilityService,
        IBomStaleSignal bomStaleSignal,
        IGlassWorkOrderRepository workOrderRepo,
        IWorkOrderRevisionService revisionService,
        IBomRecomputedOutbox? bomRecomputedOutbox = null)
    {
        _projectRepo = projectRepo;
        _composer = composer;
        _lineRepo = lineRepo;
        _availabilityService = availabilityService;
        _bomStaleSignal = bomStaleSignal;
        _workOrderRepo = workOrderRepo;
        _revisionService = revisionService;
        _bomRecomputedOutbox = bomRecomputedOutbox;
    }

    public async Task<BOMSummaryDto> Handle(RecomputeBOMCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var previousLines = await _lineRepo.ListByProjectAsync(project.Id, cancellationToken);
        var overrides = BomRecomputePreservation.CaptureOverrides(previousLines);
        var composition = await _composer.ComposeAsync(project, cancellationToken);

        var entities = composition.Lines
            .Select(line => new GlassProjectBOMLine(
                project.Id,
                line.Kind,
                line.Description,
                line.Quantity,
                line.Unit,
                line.UnitCost,
                line.Currency,
                line.RefId,
                line.Source,
                line.SortOrder,
                line.ProductId,
                line.IsService))
            .ToList();

        BomRecomputePreservation.ReapplyOverrides(entities, overrides);
        var nextSortOrder = entities.Count == 0 ? 0 : entities.Max(e => e.SortOrder) + 1;
        entities.AddRange(BomRecomputePreservation.CloneManualLines(previousLines, project.Id, nextSortOrder));

        var marginPercent = composition.Subtotal > 0m
            ? composition.MarginAmount / composition.Subtotal * 100m
            : 0m;
        // Recover the tenant tax rate the composer applied (settings.DefaultTaxRatePercent), the
        // same way marginPercent is recovered, so the recomputed totals honour the configured rate.
        var afterMargin = composition.Subtotal + composition.MarginAmount;
        var taxRatePercent = afterMargin > 0m ? composition.TaxAmount / afterMargin * 100m : 0m;
        var totals = BomQuoteTotalsCalculator.Calculate(entities, marginPercent, taxRatePercent);

        await _lineRepo.ReplaceAllForProjectAsync(project.Id, entities, cancellationToken);
        project.RecordCalculations(
            composition.TotalAreaM2,
            composition.TotalPanels,
            project.WindLoadPaCalculated ?? 0m,
            project.WeightedUValue ?? 0m,
            project.WeightedSoundDb ?? 0m);
        project.RecordTotals(totals.Subtotal, 0m, totals.TaxAmount, totals.GrandTotal);
        _projectRepo.Update(project);

        await _bomStaleSignal.SignalFreshAsync(project.Id, cancellationToken);

        await TriggerWorkOrderRevisionIfReleasedAsync(project.Id, composition, totals.GrandTotal, cancellationToken);

        var availability = await _availabilityService.CheckAsync(
            project.Id, warehouseId: null, cancellationToken: cancellationToken);
        return MapSummary(composition, entities, availability, totals);
    }

    private async Task TriggerWorkOrderRevisionIfReleasedAsync(
        Guid projectId,
        BOMCompositionResult composition,
        decimal grandTotal,
        CancellationToken cancellationToken)
    {
        var relevantWorkOrders = await _workOrderRepo.ListReleasableByProjectAsync(projectId, cancellationToken);
        if (relevantWorkOrders.Count == 0) return;

        var snapshotJson = BomSnapshotJsonBuilder.Build(composition.Lines);

        if (_bomRecomputedOutbox is not null)
        {
            foreach (var workOrder in relevantWorkOrders)
            {
                await _bomRecomputedOutbox.EnqueueAsync(
                    new BomRecomputedOutboxPayload(
                        workOrder.Id,
                        snapshotJson,
                        grandTotal,
                        Reason: "RecomputeBOM-Auto"),
                    cancellationToken);
            }
            return;
        }

        foreach (var workOrder in relevantWorkOrders)
        {
            await _revisionService.CreateRevisionAsync(
                workOrder.Id,
                snapshotJson,
                grandTotal,
                reason: "RecomputeBOM-Auto",
                cancellationToken);
        }
    }

    internal static BOMSummaryDto MapSummary(
        BOMCompositionResult composition,
        IReadOnlyList<GlassProjectBOMLine> entities,
        IReadOnlyList<StockAvailabilityRow>? availability = null,
        BomQuoteTotals? totals = null)
    {
        var lines = entities
            .Select(BomLineSummaryBuilder.MapLine)
            .ToList();

        var shortages = availability is null
            ? new List<BomShortageDto>()
            : availability
                .Where(a => a.HasShortage && a.ProductId.HasValue)
                .Select(a => new BomShortageDto(
                    a.BomLineId,
                    a.ProductId!.Value,
                    a.ProductSku,
                    a.RequiredQty,
                    a.AvailableQty,
                    a.ShortageQty,
                    a.Substitutes.Count))
                .ToList();

        return new BOMSummaryDto(
            composition.TotalAreaM2,
            composition.TotalPanels,
            composition.TotalWeightKg,
            composition.ProfileCost,
            composition.GlassCost,
            composition.HardwareCost,
            composition.LaborCost,
            composition.WasteCost,
            composition.TransportCost,
            composition.ScaffoldingCost,
            composition.CraneCost,
            totals?.Subtotal ?? composition.Subtotal,
            totals?.MarginAmount ?? composition.MarginAmount,
            totals?.TaxAmount ?? composition.TaxAmount,
            totals?.GrandTotal ?? composition.GrandTotal,
            composition.Currency,
            lines,
            HasStockShortage: shortages.Count > 0,
            Shortages: shortages);
    }
}

public class GetProjectBOMQueryHandler : IRequestHandler<GetProjectBOMQuery, BOMSummaryDto>
{
    private readonly IGlassProjectBOMLineRepository _lineRepo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IBOMComposer _composer;

    public GetProjectBOMQueryHandler(
        IGlassProjectBOMLineRepository lineRepo,
        IGlassProjectRepository projectRepo,
        IBOMComposer composer)
    {
        _lineRepo = lineRepo;
        _projectRepo = projectRepo;
        _composer = composer;
    }

    public async Task<BOMSummaryDto> Handle(GetProjectBOMQuery request, CancellationToken cancellationToken)
    {
        var lines = await _lineRepo.ListByProjectAsync(request.ProjectId, cancellationToken);
        if (lines.Count == 0)
        {
            var emptyProject = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
                ?? throw new GlassProjectNotFoundException();
            var composition = await _composer.ComposeAsync(emptyProject, cancellationToken);
            return RecomputeBOMCommandHandler.MapSummary(composition, Array.Empty<GlassProjectBOMLine>());
        }

        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        return BomLineSummaryBuilder.Build(project, lines);
    }
}

public class GetBomPreviewQueryHandler : IRequestHandler<GetBomPreviewQuery, BOMSummaryDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IBOMComposer _composer;

    public GetBomPreviewQueryHandler(IGlassProjectRepository projectRepo, IBOMComposer composer)
    {
        _projectRepo = projectRepo;
        _composer = composer;
    }

    public async Task<BOMSummaryDto> Handle(GetBomPreviewQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        // Live compose of the current scene. This is a query, so the pipeline never calls
        // SaveChanges — the catalog-product links ComposeAsync may create stay in the change
        // tracker and are discarded, keeping this endpoint side-effect-free.
        var composition = await _composer.ComposeAsync(project, cancellationToken);
        var entities = composition.Lines
            .Select(line => new GlassProjectBOMLine(
                project.Id,
                line.Kind,
                line.Description,
                line.Quantity,
                line.Unit,
                line.UnitCost,
                line.Currency,
                line.RefId,
                line.Source,
                line.SortOrder,
                line.ProductId,
                line.IsService))
            .ToList();
        var marginPercent = composition.Subtotal > 0m
            ? composition.MarginAmount / composition.Subtotal * 100m
            : 0m;
        // Recover the tenant tax rate the composer applied (settings.DefaultTaxRatePercent), the
        // same way marginPercent is recovered, so the recomputed totals honour the configured rate.
        var afterMargin = composition.Subtotal + composition.MarginAmount;
        var taxRatePercent = afterMargin > 0m ? composition.TaxAmount / afterMargin * 100m : 0m;
        var totals = BomQuoteTotalsCalculator.Calculate(entities, marginPercent, taxRatePercent);
        return RecomputeBOMCommandHandler.MapSummary(composition, entities, availability: null, totals);
    }
}

public class GenerateCuttingPlanCommandHandler : IRequestHandler<GenerateCuttingPlanCommand, CuttingReportDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IProfileSystemRepository _systemRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly ICuttingOptimizer1D _opt1D;
    private readonly ICuttingOptimizer2D _opt2D;
    private readonly IGlassProjectCuttingPlanRepository _planRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GenerateCuttingPlanCommandHandler(
        IGlassProjectRepository projectRepo,
        IProfileSystemRepository systemRepo,
        IGlassTypeRepository glassRepo,
        IGlassEnclosureSettingsRepository settingsRepo,
        ICuttingOptimizer1D opt1D,
        ICuttingOptimizer2D opt2D,
        IGlassProjectCuttingPlanRepository planRepo,
        ICurrentUserAccessor currentUser)
    {
        _projectRepo = projectRepo;
        _systemRepo = systemRepo;
        _glassRepo = glassRepo;
        _settingsRepo = settingsRepo;
        _opt1D = opt1D;
        _opt2D = opt2D;
        _planRepo = planRepo;
        _currentUser = currentUser;
    }

    public async Task<CuttingReportDto> Handle(GenerateCuttingPlanCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);

        var profileRequests = await BuildProfileRequestsAsync(project, cancellationToken);
        var glassRequests = await BuildGlassRequestsAsync(project, cancellationToken);

        var stockBarLength = settings.DefaultStockBarLengthMm;
        var sawKerf = (int)Math.Round(settings.SawKerfMm);
        var glassKerf = (int)Math.Round(settings.GlassKerfMm);

        var result1D = _opt1D.Plan(profileRequests, stockBarLength, sawKerf);
        var result2D = _opt2D.Plan(
            glassRequests,
            settings.DefaultJumboGlassWidthMm,
            settings.DefaultJumboGlassHeightMm,
            glassKerf,
            settings.GuillotineRequired);

        var userId = _currentUser.UserId ?? Guid.Empty;

        var plan1D = new GlassProjectCuttingPlan(
            project.Id,
            GlassCuttingPlanType.Profile1D,
            JsonSerializer.Serialize(result1D),
            0m,
            result1D.TotalWasteMm,
            result1D.UtilizationPercent,
            userId);
        await _planRepo.AddAsync(plan1D, cancellationToken);

        var plan2D = new GlassProjectCuttingPlan(
            project.Id,
            GlassCuttingPlanType.Glass2D,
            JsonSerializer.Serialize(result2D),
            result2D.TotalWasteMm2,
            0m,
            result2D.UtilizationPercent,
            userId);
        await _planRepo.AddAsync(plan2D, cancellationToken);

        return new CuttingReportDto(
            project.Id,
            DateTime.UtcNow,
            MapToDto(result1D),
            MapToDto(result2D));
    }

    private async Task<List<CuttingRequest1D>> BuildProfileRequestsAsync(GlassProject project, CancellationToken cancellationToken)
    {
        var systemIds = project.Runs.Select(r => r.ProfileSystemId).Distinct().ToList();
        var systems = await _systemRepo.GetWithItemsByIdsAsync(systemIds, cancellationToken);

        var groups = new Dictionary<(string Label, int LengthMm, int StockBarLengthMm), int>();
        foreach (var run in project.Runs)
        {
            if (!systems.TryGetValue(run.ProfileSystemId, out var system)) continue;
            var panelCount = Math.Max(1, run.Panels.Count);
            var railSpanMm = GlassRunPanelMath.PanelSpanMm(run.LengthMm, run.GeomArcRadiusMm, run.GeomArcSweepDeg);
            var segments = new[]
            {
                (Role: ProfileRole.Top, LengthMm: railSpanMm, Count: 1),
                (Role: ProfileRole.Bottom, LengthMm: railSpanMm, Count: 1),
                (Role: ProfileRole.SideJamb, LengthMm: run.HeightMm, Count: 2),
                (Role: ProfileRole.Sash, LengthMm: run.HeightMm, Count: 2 * panelCount),
                (Role: ProfileRole.Mullion, LengthMm: run.HeightMm, Count: Math.Max(0, panelCount - 1)),
            };
            foreach (var (role, lengthMm, count) in segments)
            {
                if (count <= 0) continue;
                var profile = system.Items.FirstOrDefault(p => p.Role == role) ?? system.Items.FirstOrDefault();
                if (profile is null) continue;
                // Bar stock is a property of the PROFILE, not of the tenant. This column existed
                // but nothing in the planning path read it, so every profile was planned against
                // the tenant default.
                var key = (profile.Code, lengthMm, profile.StockBarLengthMm);
                groups[key] = groups.TryGetValue(key, out var existing) ? existing + count : count;
            }
        }
        return groups
            .Select(kv => new CuttingRequest1D(kv.Key.Label, kv.Key.LengthMm, kv.Value, kv.Key.StockBarLengthMm))
            .ToList();
    }

    private async Task<List<CuttingRequest2D>> BuildGlassRequestsAsync(GlassProject project, CancellationToken cancellationToken)
    {
        var glassIds = project.Runs.SelectMany(r => r.Panels).Select(p => p.GlassTypeId).Distinct().ToList();
        var glassMap = await _glassRepo.GetByIdsAsync(glassIds, cancellationToken);

        var groups = new Dictionary<string, (string Label, int WidthMm, int BlankHeightMm, PanelCutShape? Shape, int NominalHeightMm, string GroupKey, int Count)>();
        foreach (var run in project.Runs)
        {
            foreach (var panel in run.Panels)
            {
                if (!glassMap.TryGetValue(panel.GlassTypeId, out var glass)) continue;
                var nominalHeight = panel.HeightMm ?? run.HeightMm;
                var shape = PanelCutShapeMapper.FromPanel(panel);
                // WHY: integer cutting optimiser needs whole-mm blanks; ceil keeps the blank ≥ silhouette.
                var blankHeight = (int)Math.Ceiling(PanelCutGeometry.BoundingHeightMm(nominalHeight, shape));
                var key = $"{glass.Code}|{panel.WidthMm}|{nominalHeight}|{PanelCutGeometry.Signature(shape)}";
                if (groups.TryGetValue(key, out var existing))
                {
                    groups[key] = (existing.Label, existing.WidthMm, existing.BlankHeightMm, existing.Shape, existing.NominalHeightMm, existing.GroupKey, existing.Count + 1);
                }
                else
                {
                    groups[key] = (
                        $"{glass.Code} {panel.WidthMm}×{blankHeight}",
                        panel.WidthMm,
                        blankHeight,
                        shape,
                        nominalHeight,
                        GlassSheetPoolKey(glass),
                        1);
                }
            }
        }
        return groups.Values
            .Select(g => new CuttingRequest2D(g.Label, g.WidthMm, g.BlankHeightMm, g.Count, g.Shape, g.NominalHeightMm)
            {
                GroupKey = g.GroupKey,
            })
            .ToList();
    }

    private static string GlassSheetPoolKey(GlassType glass) => $"{glass.Code} · {glass.ThicknessMm} mm";

    private static CuttingResult1DDto MapToDto(CuttingResult1D r) => new(
        r.StockBarLengthMm, r.KerfMm, r.TotalBars, r.TotalCuts, r.TotalUsedMm, r.TotalWasteMm, r.UtilizationPercent,
        r.Patterns.Select(p => new CuttingPattern1DDto(
            p.BarIndex, p.StockBarLengthMm,
            // WHY: a spliced rail is joined on site, so the shop floor must see the piece breakdown.
            p.Cuts.Select(c => new CuttingCut1DDto(c.Label, c.LengthMm, c.OffsetMm, c.PieceIndex, c.PieceCount)).ToList(),
            p.WasteMm)).ToList());

    private static CuttingResult2DDto MapToDto(CuttingResult2D r) => new(
        r.SheetWidthMm, r.SheetHeightMm, r.KerfMm, r.GuillotineOnly,
        r.TotalSheets, r.TotalUsedMm2, r.TotalWasteMm2, r.UtilizationPercent,
        r.Sheets.Select(s => new CuttingSheet2DDto(
            s.SheetIndex, s.WidthMm, s.HeightMm,
            s.Placements.Select(p => new CuttingPlacement2DDto(
                p.Label, p.X, p.Y, p.WidthMm, p.HeightMm, p.Rotated,
                PanelCutShapeMapper.ToDto(p.Shape, (decimal?)p.NominalHeightMm, p.WidthMm, p.HeightMm, p.Rotated))).ToList(),
            s.WasteMm2)
        {
            GroupKey = s.GroupKey,
        }).ToList(),
        r.Unplaced)
    {
        Groups = r.Groups
            .Select(g => new CuttingGroup2DDto(g.GroupKey, g.TotalSheets, g.TotalUsedMm2, g.TotalWasteMm2, g.UtilizationPercent))
            .ToList(),
    };
}

public class GetCuttingReportQueryHandler : IRequestHandler<GetCuttingReportQuery, CuttingReportDto?>
{
    private readonly IGlassProjectCuttingPlanRepository _planRepo;

    public GetCuttingReportQueryHandler(IGlassProjectCuttingPlanRepository planRepo) => _planRepo = planRepo;

    public async Task<CuttingReportDto?> Handle(GetCuttingReportQuery request, CancellationToken cancellationToken)
    {
        var plan1D = await _planRepo.GetLatestAsync(request.ProjectId, GlassCuttingPlanType.Profile1D, cancellationToken);

        // WHY scan a few rows instead of taking only the newest: before the nesting got its own
        // slot, an "optimise" wrote a nesting-shaped payload into the Glass2D row. Measured on the
        // live database, 10 of 74 Glass2D rows are that shape, and for 5 projects it is the NEWEST
        // row while a perfectly good cutting report sits just behind it. Taking only the newest
        // would report "no plan" and throw away recoverable work.
        var recent2D = await _planRepo.ListRecentAsync(
            request.ProjectId,
            GlassCuttingPlanType.Glass2D,
            Glass2DLookbackRows,
            cancellationToken);
        var plan2D = recent2D.FirstOrDefault(p => ReadGlass2D(p.PlanJson) is not null) ?? recent2D.FirstOrDefault();
        var result2D = ReadGlass2D(plan2D?.PlanJson);
        if (plan1D is null && result2D is null) return null;

        var result1D = plan1D is null
            ? new CuttingResult1DDto(0, 0, 0, 0, 0, 0, 0, Array.Empty<CuttingPattern1DDto>())
            : JsonSerializer.Deserialize<CuttingResult1DDto>(plan1D.PlanJson) ?? throw new InvalidOperationException("Invalid 1D plan JSON");

        var generatedAt = plan1D?.GeneratedAtUtc ?? plan2D?.GeneratedAtUtc ?? DateTime.UtcNow;
        return new CuttingReportDto(request.ProjectId, generatedAt, result1D, result2D ?? EmptyGlass2D);
    }

    private const int Glass2DLookbackRows = 5;

    private static CuttingResult2DDto EmptyGlass2D => new(
        0, 0, 0, false, 0, 0, 0, 0, Array.Empty<CuttingSheet2DDto>(), Array.Empty<string>());

    // WHY: pre-fix rows hold the nesting shape, which deserialises into a silent 0-sheet husk.
    private static CuttingResult2DDto? ReadGlass2D(string? planJson)
    {
        if (string.IsNullOrWhiteSpace(planJson)) return null;
        using var document = JsonDocument.Parse(planJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
        if (!document.RootElement.TryGetProperty(nameof(CuttingResult2DDto.SheetWidthMm), out _)) return null;
        return JsonSerializer.Deserialize<CuttingResult2DDto>(planJson)
            ?? throw new InvalidOperationException("Invalid 2D plan JSON");
    }
}

public class GetTechnicalSummaryQueryHandler : IRequestHandler<GetTechnicalSummaryQuery, TechnicalSummaryDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IWindZoneRepository _windRepo;
    private readonly IClimateZoneRepository _climateRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly IWindLoadCalculator _windCalculator;
    private readonly IThermalAcousticCalculator _thermalCalculator;

    public GetTechnicalSummaryQueryHandler(
        IGlassProjectRepository projectRepo,
        IWindZoneRepository windRepo,
        IClimateZoneRepository climateRepo,
        IGlassTypeRepository glassRepo,
        IWindLoadCalculator windCalculator,
        IThermalAcousticCalculator thermalCalculator)
    {
        _projectRepo = projectRepo;
        _windRepo = windRepo;
        _climateRepo = climateRepo;
        _glassRepo = glassRepo;
        _windCalculator = windCalculator;
        _thermalCalculator = thermalCalculator;
    }

    public async Task<TechnicalSummaryDto> Handle(GetTechnicalSummaryQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();

        var glassIds = project.Runs.SelectMany(r => r.Panels).Select(p => p.GlassTypeId).Distinct().ToList();
        var glassMap = await _glassRepo.GetByIdsAsync(glassIds, cancellationToken);
        var windInputs = new List<WindLoadPanelInput>();
        var thermalInputs = new List<ThermalAcousticPanelInput>();
        decimal totalArea = 0m;
        decimal totalWeight = 0m;
        int panelCount = 0;

        foreach (var run in project.Runs)
        {
            foreach (var panel in run.Panels)
            {
                if (!glassMap.TryGetValue(panel.GlassTypeId, out var glass)) continue;
                var areaM2 = (decimal)panel.WidthMm * run.HeightMm / 1_000_000m;
                totalArea += areaM2;
                totalWeight += areaM2 * glass.WeightKgPerM2;
                panelCount += 1;
                windInputs.Add(new WindLoadPanelInput(run.Id, panel.Id, areaM2, glass.ThicknessMm));
                thermalInputs.Add(new ThermalAcousticPanelInput(panel.Id, areaM2, glass.UValue, glass.SoundDb));
            }
        }

        WindLoadDto? windDto = null;
        if (project.WindZoneId.HasValue)
        {
            var zone = await _windRepo.GetByIdAsync(project.WindZoneId.Value, cancellationToken);
            if (zone is not null)
            {
                var wind = _windCalculator.Calculate(zone, project.BuildingHeightM ?? 0m, windInputs);
                windDto = new WindLoadDto(
                    wind.BasePressurePa,
                    wind.HeightFactor,
                    wind.AppliedPressurePa,
                    wind.Panels.Select(p => new WindLoadPanelDto(
                        p.RunId, p.PanelId, p.AppliedPressurePa, p.CurrentThicknessMm, p.RequiredMinThicknessMm, p.IsSufficient)).ToList());
            }
        }

        ClimateZone? climate = null;
        if (project.ClimateZoneId.HasValue)
        {
            climate = await _climateRepo.GetByIdAsync(project.ClimateZoneId.Value, cancellationToken);
        }
        var thermal = _thermalCalculator.Calculate(project, thermalInputs, climate);
        var thermalDto = new ThermalAcousticDto(
            thermal.TotalAreaM2,
            thermal.WeightedUValue,
            thermal.WeightedSoundDb,
            thermal.EstimatedWinterEnergySavingsKwh,
            thermal.EstimatedDbReductionVsOpen);

        return new TechnicalSummaryDto(
            project.Id,
            windDto,
            thermalDto,
            panelCount,
            project.Runs.Count,
            decimal.Round(totalArea, 3),
            decimal.Round(totalWeight, 2));
    }
}

public class Optimize2DNestingCommandHandler : IRequestHandler<Optimize2DNestingCommand, Glass2DNestingReportDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly IGlass2DNestingOptimizer _optimizer;
    private readonly IGlassProjectCuttingPlanRepository _planRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public Optimize2DNestingCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassEnclosureSettingsRepository settingsRepo,
        IGlass2DNestingOptimizer optimizer,
        IGlassProjectCuttingPlanRepository planRepo,
        IGlassTypeRepository glassRepo,
        ICurrentUserAccessor currentUser)
    {
        _projectRepo = projectRepo;
        _settingsRepo = settingsRepo;
        _optimizer = optimizer;
        _planRepo = planRepo;
        _glassRepo = glassRepo;
        _currentUser = currentUser;
    }

    public async Task<Glass2DNestingReportDto> Handle(Optimize2DNestingCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);

        var glassIds = project.Runs.SelectMany(r => r.Panels).Select(p => p.GlassTypeId).Distinct().ToList();
        var glassMap = await _glassRepo.GetByIdsAsync(glassIds, cancellationToken);
        var glassGroups = BuildPanelRequests(project, glassMap, request.AllowRotation);

        var options = new NestingOptions(
            Algorithm: string.IsNullOrWhiteSpace(request.Algorithm) ? "MaxRects" : request.Algorithm,
            Heuristic: string.IsNullOrWhiteSpace(request.Heuristic) ? "BestShortSideFit" : request.Heuristic,
            MinimizeSheets: request.MinimizeSheets,
            AcceptableUtilization: request.AcceptableUtilization <= 0m ? 0.85m : request.AcceptableUtilization,
            GuillotineOnly: request.GuillotineOnly || settings.GuillotineRequired);

        // WHY one optimizer run PER GLASS: a jumbo sheet is one physical pane of one glass, so 6 mm
        // and 8 mm panels can never share it. Keying the groups by size alone merged them and the
        // single shared sheet pool then nested them together — a cut plan that cannot be executed.
        var groupResults = new List<(string GlassLabel, Glass2DNestingResult Result)>();
        foreach (var group in glassGroups)
        {
            var sheets = new List<GlassSheet>
            {
                new(
                    Guid.NewGuid(),
                    settings.DefaultJumboGlassWidthMm,
                    settings.DefaultJumboGlassHeightMm,
                    settings.GlassKerfMm,
                    5m),
            };
            var groupResult = await _optimizer.OptimizeAsync(group.Panels, sheets, options, cancellationToken);
            groupResults.Add((group.GlassLabel, groupResult));
        }

        var merged = MergeGlassGroups(groupResults, options);
        var nestingResult = merged.Result;

        var userId = _currentUser.UserId ?? Guid.Empty;
        var generatedAt = DateTime.UtcNow;

        var dto = MapToReport(project.Id, generatedAt, nestingResult, merged.SheetGlassLabels);

        var plan = new GlassProjectCuttingPlan(
            project.Id,
            GlassCuttingPlanType.Glass2DNesting,
            JsonSerializer.Serialize(dto),
            nestingResult.TotalWasteAreaMm2,
            0m,
            nestingResult.TotalUtilizationPercent,
            userId);
        await _planRepo.AddAsync(plan, cancellationToken);

        return dto;
    }

    private sealed record NestingGlassGroup(Guid GlassTypeId, string GlassLabel, List<GlassPanelRequest> Panels);

    private static List<NestingGlassGroup> BuildPanelRequests(
        GlassProject project,
        IReadOnlyDictionary<Guid, GlassType> glassMap,
        bool allowRotation)
    {
        var groups = new Dictionary<string, (Guid GlassTypeId, Guid Id, string Label, decimal WidthMm, decimal BlankHeightMm, PanelCutShape? Shape, decimal NominalHeightMm, int Count)>();
        foreach (var run in project.Runs)
        {
            foreach (var panel in run.Panels)
            {
                var nominalHeight = (decimal)(panel.HeightMm ?? run.HeightMm);
                var shape = PanelCutShapeMapper.FromPanel(panel);
                var blankHeight = PanelCutGeometry.BoundingHeightMm(nominalHeight, shape);
                // The glass type LEADS the key: identical sizes in different glass are different
                // parts and must never be merged into one nesting request.
                var key = $"{panel.GlassTypeId}|{panel.WidthMm}|{nominalHeight}|{PanelCutGeometry.Signature(shape)}";
                if (groups.TryGetValue(key, out var existing))
                {
                    groups[key] = existing with { Count = existing.Count + 1 };
                }
                else
                {
                    groups[key] = (
                        panel.GlassTypeId,
                        panel.Id,
                        $"{panel.WidthMm}×{(int)Math.Ceiling(blankHeight)}",
                        panel.WidthMm,
                        blankHeight,
                        shape,
                        nominalHeight,
                        1);
                }
            }
        }

        return groups.Values
            .GroupBy(g => g.GlassTypeId)
            .Select(byGlass => new NestingGlassGroup(
                byGlass.Key,
                GlassGroupLabel(byGlass.Key, glassMap),
                byGlass
                    .Select(g => new GlassPanelRequest(
                        g.Id,
                        g.Label,
                        g.WidthMm,
                        g.BlankHeightMm,
                        g.Count,
                        allowRotation,
                        g.Shape,
                        g.NominalHeightMm))
                    .ToList()))
            .OrderBy(g => g.GlassLabel, StringComparer.Ordinal)
            .ToList();
    }

    private static string GlassGroupLabel(Guid glassTypeId, IReadOnlyDictionary<Guid, GlassType> glassMap) =>
        glassMap.TryGetValue(glassTypeId, out var glass)
            ? $"{glass.Code} · {glass.ThicknessMm} mm"
            : glassTypeId.ToString();

    private sealed record MergedNesting(Glass2DNestingResult Result, IReadOnlyList<string> SheetGlassLabels);

    // Each glass nested on its own sheet pool, then presented as one plan: sheets are re-indexed
    // end to end, each keeps the label of the glass it is cut from, and the totals are re-derived
    // so utilization still describes the whole job.
    private static MergedNesting MergeGlassGroups(
        IReadOnlyList<(string GlassLabel, Glass2DNestingResult Result)> results,
        NestingOptions options)
    {
        var sheets = new List<PlacedSheet>();
        var sheetGlassLabels = new List<string>();
        var unplaced = new List<UnplacedPanel>();
        foreach (var (glassLabel, result) in results)
        {
            foreach (var sheet in result.Sheets)
            {
                sheets.Add(sheet with { SheetIndex = sheets.Count + 1 });
                sheetGlassLabels.Add(glassLabel);
            }
            unplaced.AddRange(result.UnplacedPanels);
        }

        var totalUsed = sheets.Sum(s => s.UsedAreaMm2);
        var totalWaste = sheets.Sum(s => s.WasteAreaMm2);
        var capacity = totalUsed + totalWaste;
        var utilization = capacity == 0m ? 0m : decimal.Round(totalUsed * 100m / capacity, 3);

        return new MergedNesting(
            new Glass2DNestingResult(
                options.Algorithm,
                options.Heuristic,
                sheets,
                totalUsed,
                totalWaste,
                utilization,
                sheets.Count,
                unplaced),
            sheetGlassLabels);
    }

    private static Glass2DNestingReportDto MapToReport(
        Guid projectId,
        DateTime generatedAt,
        Glass2DNestingResult result,
        IReadOnlyList<string> sheetGlassLabels) =>
        new(
            projectId,
            generatedAt,
            result.Algorithm,
            result.Heuristic,
            result.SheetsUsed,
            result.TotalUsedAreaMm2,
            result.TotalWasteAreaMm2,
            result.TotalUtilizationPercent,
            result.Sheets.Select((s, i) => new Glass2DPlacedSheetDto(
                s.SheetId,
                s.SheetIndex,
                s.SheetWidthMm,
                s.SheetHeightMm,
                s.Panels.Select(p => new Glass2DPlacedPanelDto(
                    p.PanelId, p.Label, p.X, p.Y, p.WidthMm, p.HeightMm, p.Rotated,
                    PanelCutShapeMapper.ToDto(p.Shape, p.NominalHeightMm, p.WidthMm, p.HeightMm, p.Rotated))).ToList(),
                s.UsedAreaMm2,
                s.WasteAreaMm2,
                s.UtilizationPercent,
                i < sheetGlassLabels.Count ? sheetGlassLabels[i] : string.Empty)).ToList(),
            result.UnplacedPanels.Select(u => new Glass2DUnplacedPanelDto(
                u.PanelId, u.Label, u.WidthMm, u.HeightMm, u.Reason)).ToList());
}
