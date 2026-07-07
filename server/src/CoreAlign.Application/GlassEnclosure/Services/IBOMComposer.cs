using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record BOMLineDraft(
    GlassBOMLineKind Kind,
    Guid? RefId,
    Guid? ProductId,
    bool IsService,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    string Currency,
    string? Source,
    int SortOrder);

public record BOMCompositionResult(
    decimal TotalAreaM2,
    int TotalPanels,
    decimal TotalWeightKg,
    decimal ProfileCost,
    decimal GlassCost,
    decimal HardwareCost,
    decimal LaborCost,
    decimal WasteCost,
    decimal TransportCost,
    decimal ScaffoldingCost,
    decimal CraneCost,
    decimal Subtotal,
    decimal MarginAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string Currency,
    IReadOnlyList<BOMLineDraft> Lines);

public interface IBOMComposer
{
    Task<BOMCompositionResult> ComposeAsync(GlassProject project, CancellationToken cancellationToken = default);
}

public class BOMComposer : IBOMComposer
{
    private readonly IProfileSystemRepository _systemRepo;
    private readonly IProfileItemRepository _profileItemRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly IColorOptionRepository _colorRepo;
    private readonly IHardwareItemRepository _hardwareRepo;
    private readonly IHardwareKitRepository _hardwareKitRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly IExpressionEvaluator _evaluator;
    private readonly ICatalogProductLinker _linker;
    private readonly Fx.IFxRateProvider _fx;

    public BOMComposer(
        IProfileSystemRepository systemRepo,
        IProfileItemRepository profileItemRepo,
        IGlassTypeRepository glassRepo,
        IColorOptionRepository colorRepo,
        IHardwareItemRepository hardwareRepo,
        IHardwareKitRepository hardwareKitRepo,
        IGlassEnclosureSettingsRepository settingsRepo,
        IExpressionEvaluator evaluator,
        ICatalogProductLinker linker,
        Fx.IFxRateProvider fx)
    {
        _systemRepo = systemRepo;
        _profileItemRepo = profileItemRepo;
        _glassRepo = glassRepo;
        _colorRepo = colorRepo;
        _hardwareRepo = hardwareRepo;
        _hardwareKitRepo = hardwareKitRepo;
        _settingsRepo = settingsRepo;
        _evaluator = evaluator;
        _linker = linker;
        _fx = fx;
    }

    public async Task<BOMCompositionResult> ComposeAsync(GlassProject project, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var currency = settings.DefaultCurrency;
        // Catalog items (profile/glass/hardware) may be priced in different currencies; every cost is
        // converted to the project/base currency before being summed so the subtotal is meaningful.
        var asOf = DateTime.UtcNow;
        var lines = new List<BOMLineDraft>();
        var sortOrder = 0;

        decimal profileCost = 0m;
        decimal glassCost = 0m;
        decimal hardwareCost = 0m;
        decimal bendingCost = 0m;
        decimal totalArea = 0m;
        int totalPanels = 0;
        decimal totalWeightKg = 0m;

        foreach (var run in project.Runs)
        {
            var systemWithItems = await _systemRepo.GetWithItemsAsync(run.ProfileSystemId, cancellationToken);
            var profileItems = systemWithItems?.Items.ToList() ?? new List<ProfileItem>();
            var color = run.ColorId.HasValue
                ? await _colorRepo.GetByIdAsync(run.ColorId.Value, cancellationToken)
                : null;
            var priceModifier = 1m + (color?.PriceModifierPercent ?? 0m) / 100m;

            var lengthMeters = GlassRunPanelMath.PanelSpanMm(run.LengthMm, run.GeomArcRadiusMm, run.GeomArcSweepDeg) / 1000m;
            var heightMeters = run.HeightMm / 1000m;
            var panelCount = Math.Max(1, run.Panels.Count);
            var isArcRun = (run.GeomArcRadiusMm ?? 0) > 0 && Math.Abs(run.GeomArcSweepDeg ?? 0m) >= 0.1m;
            var glassCostFactor = isArcRun && run.ArcGlassBent ? settings.BentGlassCostFactor : 1m;

            var roleMeters = new (ProfileRole Role, decimal Meters)[]
            {
                (ProfileRole.Top, lengthMeters),
                (ProfileRole.Bottom, lengthMeters),
                (ProfileRole.SideJamb, heightMeters * 2m),
                (ProfileRole.Sash, heightMeters * 2m * panelCount),
                (ProfileRole.Mullion, heightMeters * Math.Max(0, panelCount - 1)),
            };

            foreach (var (role, meters) in roleMeters)
            {
                if (meters <= 0) continue;
                var representativeProfile = profileItems.FirstOrDefault(p => p.Role == role) ?? profileItems.FirstOrDefault();
                if (representativeProfile is null) continue;
                var rawUnitCost = representativeProfile.PricePerKg * priceModifier * representativeProfile.WeightKgPerMeter;
                var unitCost = await _fx.ConvertAsync(rawUnitCost, representativeProfile.Currency, currency, asOf, cancellationToken);
                var lineCost = meters * unitCost;
                profileCost += lineCost;
                totalWeightKg += meters * representativeProfile.WeightKgPerMeter;
                var profileLinkage = await _linker.EnsureLinkedAsync(representativeProfile, CatalogItemKind.Profile, cancellationToken);
                lines.Add(new BOMLineDraft(
                    GlassBOMLineKind.ProfileCut,
                    representativeProfile.Id,
                    profileLinkage.ProductId,
                    false,
                    $"{run.Label} · {role} · {representativeProfile.Name}",
                    decimal.Round(meters, 3),
                    "m",
                    decimal.Round(unitCost, 4),
                    currency,
                    run.Id.ToString(),
                    sortOrder++));
            }

            if (isArcRun)
            {
                var bendMeters = lengthMeters * 2m;
                bendingCost += bendMeters * settings.BendRailFeePerM;
                lines.Add(new BOMLineDraft(
                    GlassBOMLineKind.Labor,
                    null,
                    null,
                    true,
                    $"{run.Label} · Kavis ray bükme",
                    decimal.Round(bendMeters, 3),
                    "m",
                    decimal.Round(settings.BendRailFeePerM, 4),
                    currency,
                    run.Id.ToString(),
                    sortOrder++));

                var jointCount = Math.Max(0, panelCount - 1);
                if (jointCount > 0)
                {
                    lines.Add(new BOMLineDraft(
                        GlassBOMLineKind.HardwarePiece,
                        null,
                        null,
                        true,
                        $"{run.Label} · Açılı birleşim lameli",
                        jointCount,
                        "adet",
                        0m,
                        currency,
                        run.Id.ToString(),
                        sortOrder++));
                }
            }

            foreach (var panel in run.Panels)
            {
                var glass = await _glassRepo.GetByIdAsync(panel.GlassTypeId, cancellationToken);
                if (glass is null) continue;
                var shape = PanelCutShapeMapper.FromPanel(panel);
                var nominalHeight = panel.HeightMm ?? run.HeightMm;
                var areaM2 = PanelCutGeometry.NetAreaMm2(panel.WidthMm, nominalHeight, shape) / 1_000_000m;
                totalArea += areaM2;
                totalPanels += 1;
                totalWeightKg += areaM2 * glass.WeightKgPerM2;
                var rawGlassUnitCost = glass.PricePerM2 * glassCostFactor;
                var glassUnitCost = decimal.Round(await _fx.ConvertAsync(rawGlassUnitCost, glass.Currency, currency, asOf, cancellationToken), 4);
                var lineCost = areaM2 * glassUnitCost;
                glassCost += lineCost;
                var glassLinkage = await _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, cancellationToken);
                lines.Add(new BOMLineDraft(
                    GlassBOMLineKind.GlassPiece,
                    glass.Id,
                    glassLinkage.ProductId,
                    false,
                    $"{run.Label} · Panel {panel.PanelIndex + 1} · {glass.Name}",
                    decimal.Round(areaM2, 3),
                    "m²",
                    glassUnitCost,
                    currency,
                    panel.Id.ToString(),
                    sortOrder++));
            }

            var kits = await _hardwareKitRepo.ListAsync(isActive: true, systemId: run.ProfileSystemId, cancellationToken);
            foreach (var kit in kits)
            {
                var kitWithItems = await _hardwareKitRepo.GetWithItemsAsync(kit.Id, cancellationToken);
                if (kitWithItems is null) continue;
                foreach (var kitItem in kitWithItems.Items)
                {
                    var hardware = await _hardwareRepo.GetByIdAsync(kitItem.HardwareItemId, cancellationToken);
                    if (hardware is null) continue;
                    var variables = BuildExpressionVariables(run, panelCount, project);
                    if (!string.IsNullOrWhiteSpace(kitItem.ConditionExpression))
                    {
                        var condition = _evaluator.EvaluateBoolean(kitItem.ConditionExpression, variables);
                        if (!condition) continue;
                    }
                    var qty = _evaluator.EvaluateNumeric(kitItem.QuantityFormula, variables);
                    qty = Math.Max(0, decimal.Ceiling(qty));
                    if (qty <= 0) continue;
                    var hardwareUnitPrice = await _fx.ConvertAsync(hardware.UnitPrice, hardware.Currency, currency, asOf, cancellationToken);
                    var lineCost = qty * hardwareUnitPrice;
                    hardwareCost += lineCost;
                    var hardwareLinkage = await _linker.EnsureLinkedAsync(hardware, CatalogItemKind.Hardware, cancellationToken);
                    lines.Add(new BOMLineDraft(
                        GlassBOMLineKind.HardwarePiece,
                        hardware.Id,
                        hardwareLinkage.ProductId,
                        false,
                        $"{run.Label} · {kit.Name} · {hardware.Name}",
                        qty,
                        hardware.Unit,
                        decimal.Round(hardwareUnitPrice, 4),
                        currency,
                        run.Id.ToString(),
                        sortOrder++));
                }
            }
        }

        var wastePercent = settings.DefaultWastePercent / 100m;
        var wasteCost = (profileCost + glassCost) * wastePercent;
        var workshopLaborCost = totalArea * settings.LaborCostPerM2;
        var laborCost = workshopLaborCost + bendingCost;
        var transportCost = totalWeightKg * settings.TransportRatePerKg + settings.TransportRatePerKm;
        var floor = project.FloorNumber ?? 0;
        var scaffoldingCost = floor >= settings.ScaffoldingRequiredFromFloor
            ? totalArea * settings.ScaffoldingRatePerM2
            : 0m;
        var craneCost = floor >= settings.CraneRequiredFromFloor
            ? floor * 3m * settings.CraneRatePerMeter
            : 0m;

        if (wasteCost > 0)
            lines.Add(new BOMLineDraft(GlassBOMLineKind.ProfileCut, null, null, true, "Waste allowance", 1m, "lot", decimal.Round(wasteCost, 4), currency, null, sortOrder++));
        if (workshopLaborCost > 0)
            lines.Add(new BOMLineDraft(GlassBOMLineKind.Labor, null, null, true, "Workshop labor", decimal.Round(totalArea, 3), "m²", settings.LaborCostPerM2, currency, null, sortOrder++));
        if (transportCost > 0)
            lines.Add(new BOMLineDraft(GlassBOMLineKind.Transport, null, null, true, "Transport", 1m, "trip", decimal.Round(transportCost, 4), currency, null, sortOrder++));
        if (scaffoldingCost > 0)
            lines.Add(new BOMLineDraft(GlassBOMLineKind.Installation, null, null, true, "Scaffolding", decimal.Round(totalArea, 3), "m²", settings.ScaffoldingRatePerM2, currency, null, sortOrder++));
        if (craneCost > 0)
            lines.Add(new BOMLineDraft(GlassBOMLineKind.Installation, null, null, true, "Crane", floor * 3m, "m", settings.CraneRatePerMeter, currency, null, sortOrder++));

        var subtotal = profileCost + glassCost + hardwareCost + wasteCost + laborCost + transportCost + scaffoldingCost + craneCost;
        var marginAmount = subtotal * (settings.DefaultMarginPercent / 100m);
        var afterMargin = subtotal + marginAmount;
        var taxAmount = afterMargin * (settings.DefaultTaxRatePercent / 100m);
        var grandTotal = afterMargin + taxAmount;

        return new BOMCompositionResult(
            decimal.Round(totalArea, 3),
            totalPanels,
            decimal.Round(totalWeightKg, 2),
            decimal.Round(profileCost, 4),
            decimal.Round(glassCost, 4),
            decimal.Round(hardwareCost, 4),
            decimal.Round(laborCost, 4),
            decimal.Round(wasteCost, 4),
            decimal.Round(transportCost, 4),
            decimal.Round(scaffoldingCost, 4),
            decimal.Round(craneCost, 4),
            decimal.Round(subtotal, 4),
            decimal.Round(marginAmount, 4),
            decimal.Round(taxAmount, 4),
            decimal.Round(grandTotal, 4),
            currency,
            lines);
    }

    private static IReadOnlyDictionary<string, object> BuildExpressionVariables(
        GlassProjectRun run,
        int panelCount,
        GlassProject project)
    {
        var openings = run.Panels.Select(p => p.OpeningType).ToList();
        return new Dictionary<string, object>
        {
            ["panel_count"] = (decimal)panelCount,
            ["run_length_mm"] = (decimal)run.LengthMm,
            ["run_developed_length_mm"] = (decimal)GlassRunPanelMath.PanelSpanMm(run.LengthMm, run.GeomArcRadiusMm, run.GeomArcSweepDeg),
            ["run_height_mm"] = (decimal)run.HeightMm,
            ["opening_count_folding"] = (decimal)openings.Count(o => o == GlassOpeningType.Folding),
            ["opening_count_sliding"] = (decimal)openings.Count(o => o == GlassOpeningType.SlidingLeft || o == GlassOpeningType.SlidingRight),
            ["opening_count_hinged"] = (decimal)openings.Count(o => o == GlassOpeningType.Hinged),
            ["glass_thickness_mm"] = (decimal)0,
            ["floor_number"] = (decimal)(project.FloorNumber ?? 0),
        };
    }
}
