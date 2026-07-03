using System.Text.Json;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface ISceneValidator
{
    Task<GlassProjectValidationResultDto> ValidateAsync(GlassProject project, CancellationToken cancellationToken = default);
}

public class SceneValidator : ISceneValidator
{
    // WHY: thermally bent tempered glass is typically limited to a minimum radius of ~150x its thickness
    private const int MinBendRadiusPerThicknessMm = 150;

    private readonly IProfileSystemRepository _systemRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly IWindZoneRepository _windRepo;
    private readonly IClimateZoneRepository _climateRepo;

    public SceneValidator(
        IProfileSystemRepository systemRepo,
        IGlassTypeRepository glassRepo,
        IWindZoneRepository windRepo,
        IClimateZoneRepository climateRepo)
    {
        _systemRepo = systemRepo;
        _glassRepo = glassRepo;
        _windRepo = windRepo;
        _climateRepo = climateRepo;
    }

    public async Task<GlassProjectValidationResultDto> ValidateAsync(GlassProject project, CancellationToken cancellationToken = default)
    {
        var findings = new List<GlassValidationFindingDto>();
        var systems = new Dictionary<Guid, ProfileSystem>();
        var glassTypes = new Dictionary<Guid, GlassType>();

        foreach (var run in project.Runs)
        {
            if (!systems.TryGetValue(run.ProfileSystemId, out var system))
            {
                var loaded = await _systemRepo.GetByIdAsync(run.ProfileSystemId, cancellationToken);
                if (loaded is null)
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.System.NotFound",
                        "GlassEnclosure.Validation.SystemNotFound",
                        null, run.Id, null));
                    continue;
                }
                systems[run.ProfileSystemId] = loaded;
                system = loaded;
            }

            var supportedThicknesses = ParseIntArray(system.SupportedGlassThicknessesJson);
            var supportedOpenings = ParseStringArray(system.SupportedOpeningsJson);

            foreach (var panel in run.Panels)
            {
                if (!glassTypes.TryGetValue(panel.GlassTypeId, out var glass))
                {
                    var loaded = await _glassRepo.GetByIdAsync(panel.GlassTypeId, cancellationToken);
                    if (loaded is null)
                    {
                        findings.Add(new GlassValidationFindingDto(
                            GlassValidationSeverity.Error,
                            "GE.Glass.NotFound",
                            "GlassEnclosure.Validation.GlassNotFound",
                            null, run.Id, panel.Id));
                        continue;
                    }
                    glassTypes[panel.GlassTypeId] = loaded;
                    glass = loaded;
                }

                if (panel.WidthMm > system.MaxPanelWidthMm)
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.Panel.TooWide",
                        "GlassEnclosure.Validation.PanelTooWide",
                        $"{panel.WidthMm}|{system.MaxPanelWidthMm}",
                        run.Id, panel.Id));
                }

                if (run.HeightMm > system.MaxPanelHeightMm)
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.Run.TooTall",
                        "GlassEnclosure.Validation.RunTooTall",
                        $"{run.HeightMm}|{system.MaxPanelHeightMm}",
                        run.Id, null));
                }

                if (!supportedThicknesses.Contains(glass.ThicknessMm))
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.Glass.ThicknessNotSupported",
                        "GlassEnclosure.Validation.GlassThicknessMismatch",
                        $"{glass.ThicknessMm}|{string.Join(",", supportedThicknesses)}",
                        run.Id, panel.Id));
                }

                if (!supportedOpenings.Contains(panel.OpeningType.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.System.OpeningMismatch",
                        "GlassEnclosure.Validation.SystemOpeningMismatch",
                        $"{system.Name}|{panel.OpeningType}",
                        run.Id, panel.Id));
                }

                var panelAreaM2 = (decimal)panel.WidthMm * run.HeightMm / 1_000_000m;
                if (glass.MaxPanelAreaM2 > 0 && panelAreaM2 > glass.MaxPanelAreaM2)
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.Glass.AreaExceedsMax",
                        "GlassEnclosure.Validation.GlassAreaExceedsMax",
                        $"{panelAreaM2:F2}|{glass.MaxPanelAreaM2:F2}",
                        run.Id, panel.Id));
                }

                var panelWeight = panelAreaM2 * glass.WeightKgPerM2;
                if (panelWeight > system.MaxPanelWeightKg)
                {
                    findings.Add(new GlassValidationFindingDto(
                        GlassValidationSeverity.Error,
                        "GE.Panel.WeightExceeds",
                        "GlassEnclosure.Validation.WeightExceeds",
                        $"{panelWeight:F2}|{system.MaxPanelWeightKg:F2}",
                        run.Id, panel.Id));
                }

                if (run.ArcGlassBent
                    && run.GeomArcRadiusMm is int bendRadiusMm
                    && bendRadiusMm > 0
                    && Math.Abs(run.GeomArcSweepDeg ?? 0m) >= 0.1m)
                {
                    var minBendRadiusMm = glass.ThicknessMm * MinBendRadiusPerThicknessMm;
                    if (bendRadiusMm < minBendRadiusMm)
                    {
                        findings.Add(new GlassValidationFindingDto(
                            GlassValidationSeverity.Warning,
                            "GE.Arc.BendRadiusTight",
                            "GlassEnclosure.Validation.BendRadiusTight",
                            $"{bendRadiusMm}|{minBendRadiusMm}",
                            run.Id, panel.Id));
                    }
                }

                if (project.WindZoneId.HasValue)
                {
                    var windZone = await _windRepo.GetByIdAsync(project.WindZoneId.Value, cancellationToken);
                    var heightM = project.BuildingHeightM ?? 10m;
                    if (windZone is not null)
                    {
                        var heightFactor = 1m + (heightM / 100m) * windZone.HeightFactorMultiplier;
                        var pressurePa = windZone.BaseWindPressurePa * heightFactor;
                        if (glass.AllowablePressurePa > 0 && pressurePa > glass.AllowablePressurePa)
                        {
                            findings.Add(new GlassValidationFindingDto(
                                GlassValidationSeverity.Error,
                                "GE.WindLoad.GlassInsufficient",
                                "GlassEnclosure.Validation.WindLoadFail",
                                $"{pressurePa:F0}|{glass.AllowablePressurePa:F0}",
                                run.Id, panel.Id));
                        }
                    }
                }
            }
        }

        if (project.ClimateZoneId.HasValue)
        {
            var climateZone = await _climateRepo.GetByIdAsync(project.ClimateZoneId.Value, cancellationToken);
            if (climateZone is not null && climateZone.RecommendsSeismicSmallerPanel)
            {
                foreach (var run in project.Runs)
                {
                    foreach (var panel in run.Panels)
                    {
                        var areaM2 = (decimal)panel.WidthMm * run.HeightMm / 1_000_000m;
                        if (areaM2 > 2m)
                        {
                            findings.Add(new GlassValidationFindingDto(
                                GlassValidationSeverity.Warning,
                                "GE.Seismic.PanelTooLarge",
                                "GlassEnclosure.Validation.SeismicTooLarge",
                                $"{areaM2:F2}|2.00",
                                run.Id, panel.Id));
                        }
                    }
                }
            }
        }

        foreach (var conn in project.Connections)
        {
            if (conn.MitreCutDeg < 10m || conn.MitreCutDeg > 80m)
            {
                findings.Add(new GlassValidationFindingDto(
                    GlassValidationSeverity.Warning,
                    "GE.Connection.AngleOutOfRange",
                    "GlassEnclosure.Validation.ConnectionAngleInvalid",
                    $"{conn.MitreCutDeg}",
                    null, null));
            }
        }

        return new GlassProjectValidationResultDto(findings);
    }

    private static IReadOnlyList<int> ParseIntArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch (JsonException)
        {
            return Array.Empty<int>();
        }
    }

    private static IReadOnlyList<string> ParseStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
