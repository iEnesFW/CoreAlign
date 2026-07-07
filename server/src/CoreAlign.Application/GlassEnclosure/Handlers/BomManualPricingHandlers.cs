using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class OverrideBomLinePriceCommandHandler : IRequestHandler<OverrideBomLinePriceCommand, BOMSummaryDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectBOMLineRepository _lineRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public OverrideBomLinePriceCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectBOMLineRepository lineRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _projectRepo = projectRepo;
        _lineRepo = lineRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task<BOMSummaryDto> Handle(OverrideBomLinePriceCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var lines = await _lineRepo.ListByProjectForUpdateAsync(project.Id, cancellationToken);
        var line = lines.FirstOrDefault(l => l.Id == request.LineId)
            ?? throw new GlassBomLineNotFoundException();

        line.ApplyUnitPriceOverride(request.UnitPriceOverride);

        await BomTotalsRecorder.RecordAsync(_settingsRepo, _projectRepo, project, lines, cancellationToken);
        return BomLineSummaryBuilder.Build(project, lines);
    }
}

public class AddManualBomLineCommandHandler : IRequestHandler<AddManualBomLineCommand, BOMSummaryDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectBOMLineRepository _lineRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public AddManualBomLineCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectBOMLineRepository lineRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _projectRepo = projectRepo;
        _lineRepo = lineRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task<BOMSummaryDto> Handle(AddManualBomLineCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var lines = await _lineRepo.ListByProjectForUpdateAsync(project.Id, cancellationToken);
        var sortOrder = lines.Count == 0 ? 0 : lines.Max(l => l.SortOrder) + 1;

        var line = new GlassProjectBOMLine(
            project.Id,
            request.Data.Kind ?? GlassBOMLineKind.HardwarePiece,
            request.Data.Description.Trim(),
            request.Data.Quantity,
            request.Data.Unit.Trim(),
            request.Data.UnitPrice,
            project.Currency,
            source: "Manual",
            sortOrder: sortOrder,
            isManual: true);
        await _lineRepo.AddAsync(line, cancellationToken);

        var allLines = lines.Append(line).ToList();
        await BomTotalsRecorder.RecordAsync(_settingsRepo, _projectRepo, project, allLines, cancellationToken);
        return BomLineSummaryBuilder.Build(project, allLines);
    }
}

public class DeleteManualBomLineCommandHandler : IRequestHandler<DeleteManualBomLineCommand, BOMSummaryDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectBOMLineRepository _lineRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public DeleteManualBomLineCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectBOMLineRepository lineRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _projectRepo = projectRepo;
        _lineRepo = lineRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task<BOMSummaryDto> Handle(DeleteManualBomLineCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var lines = await _lineRepo.ListByProjectForUpdateAsync(project.Id, cancellationToken);
        var line = lines.FirstOrDefault(l => l.Id == request.LineId)
            ?? throw new GlassBomLineNotFoundException();
        if (!line.IsManual) throw new GlassBomLineNotManualException();

        _lineRepo.Remove(line);

        var remaining = lines.Where(l => l.Id != line.Id).ToList();
        await BomTotalsRecorder.RecordAsync(_settingsRepo, _projectRepo, project, remaining, cancellationToken);
        return BomLineSummaryBuilder.Build(project, remaining);
    }
}

public class PushBomLinePriceToCatalogCommandHandler : IRequestHandler<PushBomLinePriceToCatalogCommand, PushBomLinePriceResultDto>
{
    private readonly IGlassProjectBOMLineRepository _lineRepo;
    private readonly IProfileItemRepository _profileRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly IHardwareItemRepository _hardwareRepo;

    public PushBomLinePriceToCatalogCommandHandler(
        IGlassProjectBOMLineRepository lineRepo,
        IProfileItemRepository profileRepo,
        IGlassTypeRepository glassRepo,
        IHardwareItemRepository hardwareRepo)
    {
        _lineRepo = lineRepo;
        _profileRepo = profileRepo;
        _glassRepo = glassRepo;
        _hardwareRepo = hardwareRepo;
    }

    public async Task<PushBomLinePriceResultDto> Handle(PushBomLinePriceToCatalogCommand request, CancellationToken cancellationToken)
    {
        var lines = await _lineRepo.ListByProjectForUpdateAsync(request.ProjectId, cancellationToken);
        var line = lines.FirstOrDefault(l => l.Id == request.LineId)
            ?? throw new GlassBomLineNotFoundException();
        if (line.IsManual || line.IsService || !line.RefId.HasValue)
            throw new GlassBomLinePushNotAllowedException("line is not linked to a catalog item.");
        if (!line.UnitPriceOverride.HasValue)
            throw new GlassBomLinePushNotAllowedException("line has no manual price override.");

        var pushedPrice = line.UnitPriceOverride.Value;
        var newCatalogPrice = await UpdateCatalogPriceAsync(line.Kind, line.RefId.Value, pushedPrice, cancellationToken);

        line.AdoptOverrideAsUnitCost();

        return new PushBomLinePriceResultDto(
            line.Id, line.RefId.Value, line.Kind, pushedPrice, newCatalogPrice, line.Currency);
    }

    private async Task<decimal> UpdateCatalogPriceAsync(
        GlassBOMLineKind kind,
        Guid refId,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case GlassBOMLineKind.GlassPiece:
                var glass = await _glassRepo.GetByIdAsync(refId, cancellationToken)
                    ?? throw new GlassEnclosureNotFoundException("Glass type");
                glass.UpdatePricePerM2(unitPrice);
                _glassRepo.Update(glass);
                return unitPrice;
            case GlassBOMLineKind.HardwarePiece:
                var hardware = await _hardwareRepo.GetByIdAsync(refId, cancellationToken)
                    ?? throw new GlassEnclosureNotFoundException("Hardware item");
                hardware.UpdateUnitPrice(unitPrice);
                _hardwareRepo.Update(hardware);
                return unitPrice;
            case GlassBOMLineKind.ProfileCut:
                var profile = await _profileRepo.GetByIdAsync(refId, cancellationToken)
                    ?? throw new GlassEnclosureNotFoundException("Profile item");
                if (profile.WeightKgPerMeter <= 0m)
                    throw new GlassBomLinePushNotAllowedException("profile weight per meter is not defined.");
                var pricePerKg = decimal.Round(unitPrice / profile.WeightKgPerMeter, 4);
                profile.UpdatePricePerKg(pricePerKg);
                _profileRepo.Update(profile);
                return pricePerKg;
            default:
                throw new GlassBomLinePushNotAllowedException($"line kind '{kind}' has no catalog price.");
        }
    }
}

internal static class BomTotalsRecorder
{
    internal static async Task RecordAsync(
        IGlassEnclosureSettingsRepository settingsRepo,
        IGlassProjectRepository projectRepo,
        GlassProject project,
        IReadOnlyList<GlassProjectBOMLine> lines,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var totals = BomQuoteTotalsCalculator.Calculate(
            lines, settings.DefaultMarginPercent, settings.DefaultTaxRatePercent);
        project.RecordTotals(totals.Subtotal, 0m, totals.TaxAmount, totals.GrandTotal);
        projectRepo.Update(project);
    }
}
