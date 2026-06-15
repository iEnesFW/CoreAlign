using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Queries;

public record GetEnclosurePresetsQuery(EnclosureCategory? Category = null)
    : IRequest<IReadOnlyList<EnclosurePresetDto>>;

public sealed record EnclosurePresetDto(
    string Subtype,
    string Category,
    string DefaultGeometryMode,
    string DefaultMountingTopology,
    string DefaultConnectorKind,
    int? DefaultPanelWidthMm,
    int? DefaultPanelHeightMm,
    int? DefaultPanelCount,
    decimal? DefaultRoofPitchDeg,
    string? Notes);

public class GetEnclosurePresetsQueryHandler
    : IRequestHandler<GetEnclosurePresetsQuery, IReadOnlyList<EnclosurePresetDto>>
{
    private readonly ITemplateRegistry _templateRegistry;

    public GetEnclosurePresetsQueryHandler(ITemplateRegistry templateRegistry)
    {
        _templateRegistry = templateRegistry;
    }

    public Task<IReadOnlyList<EnclosurePresetDto>> Handle(GetEnclosurePresetsQuery request, CancellationToken cancellationToken)
    {
        var source = request.Category.HasValue
            ? _templateRegistry.ListByCategory(request.Category.Value)
            : _templateRegistry.All;

        IReadOnlyList<EnclosurePresetDto> result = source
            .Select(MapPreset)
            .ToList();

        return Task.FromResult(result);
    }

    private static EnclosurePresetDto MapPreset(IEnclosurePreset preset)
    {
        var defaults = preset.BuildDefaults();
        return new EnclosurePresetDto(
            preset.Subtype.ToString(),
            preset.Category.ToString(),
            preset.DefaultGeometryMode.ToString(),
            preset.DefaultMountingTopology.ToString(),
            preset.DefaultConnectorKind.ToString(),
            defaults.DefaultPanelWidthMm,
            defaults.DefaultPanelHeightMm,
            defaults.DefaultPanelCount,
            defaults.DefaultRoofPitchDeg,
            defaults.Notes);
    }
}
