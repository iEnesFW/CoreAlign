using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Application.GlassPlates.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CoreAlign.Application.GlassPlates.Handlers;

public class ConsumeGlassPlateHandler : IRequestHandler<ConsumeGlassPlateCommand, ConsumeGlassPlateResultDto>
{
    private const decimal AreaToleranceMm2 = 1m;
    private const string BelowMinReasonCode = "SCR-BELOWMIN";

    private readonly IGlassPlateRepository _plates;
    private readonly IGlassPlateConsumptionRepository _consumptions;
    private readonly IProductRepository _products;
    private readonly IAllocationService _allocation;
    private readonly IStockReasonCodeRepository _reasons;
    private readonly IGLPostingOutbox _outbox;
    private readonly IConfiguration _configuration;
    private readonly IGlassPlateDepletionNotifier _notifier;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    private static readonly HashSet<StockReasonCategory> WriteOffCategories = new()
    {
        StockReasonCategory.DamageWriteOff,
        StockReasonCategory.Expired,
        StockReasonCategory.Loss,
        StockReasonCategory.Scrap,
    };

    public ConsumeGlassPlateHandler(
        IGlassPlateRepository plates,
        IGlassPlateConsumptionRepository consumptions,
        IProductRepository products,
        IAllocationService allocation,
        IStockReasonCodeRepository reasons,
        IGLPostingOutbox outbox,
        IConfiguration configuration,
        IGlassPlateDepletionNotifier notifier,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _plates = plates;
        _consumptions = consumptions;
        _products = products;
        _allocation = allocation;
        _reasons = reasons;
        _outbox = outbox;
        _configuration = configuration;
        _notifier = notifier;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<ConsumeGlassPlateResultDto> Handle(ConsumeGlassPlateCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var plate = await _plates.GetByIdAsync(tenantId, c.PlateId, ct)
            ?? throw new GlassPlateNotFoundException(c.PlateId);
        var product = await _products.GetByIdAsync(plate.ProductId, ct)
            ?? throw new ProductNotFoundException();

        var wasAvailable = plate.Status == GlassPlateStatus.Available;
        var availableBefore = await _plates.CountAvailableAsync(tenantId, plate.ProductId, plate.WarehouseId, ct);

        var remaining = plate.RemainingAreaMm2;
        var cut = c.CutAreaMm2;
        if (cut > remaining + AreaToleranceMm2)
        {
            throw new GlassPlateAreaExceededException(cut, remaining);
        }
        cut = Math.Min(cut, remaining);
        var leftover = remaining - cut;
        var occurredAt = DateTime.UtcNow;

        GlassPlate? remnant = null;
        var remnantArea = 0m;
        if (c.RemnantWidthMm is > 0m && c.RemnantHeightMm is > 0m && leftover >= AreaToleranceMm2)
        {
            var candidateArea = Math.Min(c.RemnantWidthMm.Value * c.RemnantHeightMm.Value, remaining);
            if (MeetsMinimum(product, c.RemnantWidthMm.Value, c.RemnantHeightMm.Value, candidateArea))
            {
                remnantArea = candidateArea;
                var number = string.IsNullOrWhiteSpace(c.RemnantPlateNumber)
                    ? $"{plate.PlateNumber}-R"
                    : c.RemnantPlateNumber.Trim();
                remnant = plate.CreateRemnant(number, c.RemnantWidthMm.Value, c.RemnantHeightMm.Value, occurredAt);
            }
        }

        decimal productionArea;
        decimal scrapArea;
        if (remnant is not null)
        {
            productionArea = remaining - remnantArea;
            scrapArea = 0m;
        }
        else if (leftover < AreaToleranceMm2)
        {
            productionArea = remaining;
            scrapArea = 0m;
        }
        else
        {
            productionArea = cut;
            scrapArea = leftover;
        }

        var issue = await _allocation.ApplyIssueAsync(new StockIssueRequest(
            ProductId: plate.ProductId,
            WarehouseId: plate.WarehouseId,
            Quantity: productionArea / 1_000_000m,
            SourceDocumentType: StockSourceDocumentType.Production,
            SourceDocumentId: c.JobId,
            SourceLineId: c.OrderLineId,
            SourceReference: "glass-plate-consume",
            LotId: plate.LotId,
            SerialNumber: null,
            ReasonCodeId: null,
            Notes: null,
            PostedByUserId: c.PostedByUserId), ct);

        var scrappedAreaMm2 = 0m;
        Guid? scrapReasonId = null;
        if (scrapArea > AreaToleranceMm2)
        {
            var reason = await _reasons.GetByCodeAsync(BelowMinReasonCode, ct);
            scrapReasonId = reason?.Id;
            var scrapMovement = await _allocation.AdjustAsync(new StockAdjustmentRequest(
                ProductId: plate.ProductId,
                WarehouseId: plate.WarehouseId,
                Delta: -(scrapArea / 1_000_000m),
                UnitCost: null,
                SourceDocumentType: StockSourceDocumentType.Production,
                SourceDocumentId: null,
                ReasonCodeId: scrapReasonId,
                Notes: "below-minimum offcut",
                LotId: plate.LotId,
                PostedByUserId: c.PostedByUserId), ct);
            await EnqueueWriteOffAsync(reason, scrapMovement, ct);
            scrappedAreaMm2 = scrapArea;
        }

        if (remnant is not null)
        {
            await _plates.AddAsync(remnant, ct);
        }

        plate.MarkConsumed(occurredAt);

        var consumption = new GlassPlateConsumption(
            plate.Id,
            plate.ProductId,
            plate.WarehouseId,
            cutAreaMm2: cut,
            pieces: c.Pieces,
            occurredAt,
            c.PostedByUserId,
            orderLineId: c.OrderLineId,
            jobId: c.JobId,
            cutWidthMm: c.CutWidthMm,
            cutHeightMm: c.CutHeightMm,
            resultingRemnantPlateId: remnant?.Id,
            scrappedAreaMm2: scrappedAreaMm2,
            scrapReasonCodeId: scrapReasonId,
            workCenterId: c.WorkCenterId,
            operatorId: c.OperatorId,
            stockMovementId: issue.Id);
        await _consumptions.AddAsync(consumption, ct);

        var availableAfter = availableBefore - (wasAvailable ? 1 : 0) + (remnant is not null ? 1 : 0);
        await _notifier.NotifyIfDepletedAsync(tenantId, product, plate.WarehouseId, availableAfter, c.PostedByUserId, ct);

        await _uow.SaveChangesAsync(ct);

        return new ConsumeGlassPlateResultDto(
            issue.Id,
            productionArea,
            remnant?.Id,
            remnantArea,
            scrappedAreaMm2);
    }

    private bool MeetsMinimum(Product product, decimal widthMm, decimal heightMm, decimal areaMm2)
    {
        if (product.MinRemnantWidthMm is > 0m || product.MinRemnantHeightMm is > 0m)
        {
            return widthMm >= (product.MinRemnantWidthMm ?? 0m)
                && heightMm >= (product.MinRemnantHeightMm ?? 0m);
        }

        var minArea = product.MinRemnantAreaMm2
            ?? _configuration.GetValue<decimal?>("GlassPlateTracking:MinRemnantAreaMm2");
        return minArea is not > 0m || areaMm2 >= minArea.Value;
    }

    private async Task EnqueueWriteOffAsync(StockReasonCode? reason, StockMovement movement, CancellationToken ct)
    {
        if (reason is null || !reason.AffectsCost || !WriteOffCategories.Contains(reason.Category)) return;

        var amount = Math.Round(movement.Quantity * movement.UnitCost, 4);
        if (amount <= 0m) return;

        var lines = new[]
        {
            new GLPostingLine(GLPostingKey.InventoryWriteOff, amount, 0m),
            new GLPostingLine(GLPostingKey.Inventory, 0m, amount),
        };

        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.InventoryWriteOff,
            movement.Id,
            reason.Code,
            movement.OccurredAtUtc.Date,
            JournalEntryType.Mahsup,
            $"Cam plaka eşik-altı fire ({reason.Code})",
            lines), ct);
    }
}
