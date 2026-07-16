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

namespace CoreAlign.Application.GlassPlates.Handlers;

public class ScrapGlassPlateHandler : IRequestHandler<ScrapGlassPlateCommand, GlassScrapResultDto>
{
    private readonly IGlassPlateRepository _plates;
    private readonly IGlassPlateConsumptionRepository _consumptions;
    private readonly IProductRepository _products;
    private readonly IAllocationService _allocation;
    private readonly IStockReasonCodeRepository _reasons;
    private readonly IGLPostingOutbox _outbox;
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

    public ScrapGlassPlateHandler(
        IGlassPlateRepository plates,
        IGlassPlateConsumptionRepository consumptions,
        IProductRepository products,
        IAllocationService allocation,
        IStockReasonCodeRepository reasons,
        IGLPostingOutbox outbox,
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
        _notifier = notifier;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<GlassScrapResultDto> Handle(ScrapGlassPlateCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var occurredAt = DateTime.UtcNow;

        Guid productId;
        Guid warehouseId;
        Guid? lotId = null;
        decimal scrapAreaMm2;
        var platesScrapped = 0;
        var wasAvailable = false;
        var availableBefore = 0;
        GlassPlate? plate = null;

        if (c.PlateId is not null)
        {
            plate = await _plates.GetByIdAsync(tenantId, c.PlateId.Value, ct)
                ?? throw new GlassPlateNotFoundException(c.PlateId.Value);
            productId = plate.ProductId;
            warehouseId = plate.WarehouseId;
            lotId = plate.LotId;
            wasAvailable = plate.Status == GlassPlateStatus.Available;
            availableBefore = await _plates.CountAvailableAsync(tenantId, productId, warehouseId, ct);

            if (c.Mode == GlassScrapMode.Count || (c.AreaMm2 ?? 0m) >= plate.RemainingAreaMm2)
            {
                scrapAreaMm2 = plate.RemainingAreaMm2;
                plate.Scrap(occurredAt);
                platesScrapped = 1;
            }
            else
            {
                scrapAreaMm2 = c.AreaMm2 ?? 0m;
                plate.ConsumeArea(scrapAreaMm2, occurredAt);
            }
        }
        else
        {
            productId = c.ProductId!.Value;
            warehouseId = c.WarehouseId!.Value;
            scrapAreaMm2 = c.AreaMm2 ?? 0m;
        }

        var areaM2 = scrapAreaMm2 / 1_000_000m;

        var movement = await _allocation.AdjustAsync(new StockAdjustmentRequest(
            ProductId: productId,
            WarehouseId: warehouseId,
            Delta: -areaM2,
            UnitCost: null,
            SourceDocumentType: StockSourceDocumentType.Adjustment,
            SourceDocumentId: null,
            ReasonCodeId: c.ReasonCodeId,
            Notes: c.Notes,
            LotId: lotId,
            PostedByUserId: c.PostedByUserId), ct);

        await EnqueueWriteOffAsync(c.ReasonCodeId, movement, ct);

        if (plate is not null)
        {
            var consumption = new GlassPlateConsumption(
                plate.Id,
                productId,
                warehouseId,
                cutAreaMm2: 0m,
                pieces: 0,
                occurredAt,
                c.PostedByUserId,
                scrappedAreaMm2: scrapAreaMm2,
                scrapReasonCodeId: c.ReasonCodeId,
                workCenterId: c.WorkCenterId,
                operatorId: c.OperatorId,
                stockMovementId: movement.Id);
            await _consumptions.AddAsync(consumption, ct);

            var product = await _products.GetByIdAsync(plate.ProductId, ct);
            if (product is not null)
            {
                var availableAfter = availableBefore - (platesScrapped == 1 && wasAvailable ? 1 : 0);
                await _notifier.NotifyIfDepletedAsync(tenantId, product, plate.WarehouseId, availableAfter, c.PostedByUserId, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return new GlassScrapResultDto(movement.Id, scrapAreaMm2, platesScrapped);
    }

    private async Task EnqueueWriteOffAsync(Guid reasonCodeId, StockMovement movement, CancellationToken ct)
    {
        var reason = await _reasons.GetByIdAsync(reasonCodeId, ct);
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
            $"Cam plaka fire ({reason.Code})",
            lines), ct);
    }
}
