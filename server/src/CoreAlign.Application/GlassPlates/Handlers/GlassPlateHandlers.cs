using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Application.GlassPlates.Mapping;
using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Handlers;

public class ReceiveGlassPlatesHandler : IRequestHandler<ReceiveGlassPlatesCommand, ReceiveGlassPlatesResultDto>
{
    private readonly IProductRepository _products;
    private readonly IGlassPlateRepository _plates;
    private readonly IAllocationService _allocation;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public ReceiveGlassPlatesHandler(
        IProductRepository products,
        IGlassPlateRepository plates,
        IAllocationService allocation,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _products = products;
        _plates = plates;
        _allocation = allocation;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<ReceiveGlassPlatesResultDto> Handle(ReceiveGlassPlatesCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var product = await _products.GetByIdAsync(c.ProductId, ct)
            ?? throw new ProductNotFoundException();
        if (!product.IsPlateTracked)
        {
            throw new GlassPlateNotTrackedException(c.ProductId);
        }

        var numbers = c.Plates.Select(p => p.PlateNumber.Trim()).ToList();
        var duplicate = numbers
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new GlassPlateNumberConflictException(duplicate.Key);
        }

        var existing = await _plates.GetExistingPlateNumbersAsync(tenantId, numbers, ct);
        if (existing.Count > 0)
        {
            throw new GlassPlateNumberConflictException(existing[0]);
        }

        var totalAreaM2 = c.Plates.Sum(p => p.WidthMm * p.HeightMm) / 1_000_000m;
        var receivedAt = DateTime.UtcNow;

        var movement = await _allocation.ApplyReceiptAsync(new StockReceiptRequest(
            ProductId: c.ProductId,
            WarehouseId: c.WarehouseId,
            Quantity: totalAreaM2,
            UnitCost: c.UnitCostPerM2,
            SourceDocumentType: StockSourceDocumentType.Adjustment,
            SourceDocumentId: null,
            SourceLineId: null,
            SourceReference: "glass-plate-receipt",
            LotId: c.LotId,
            SerialNumber: null,
            ReasonCodeId: null,
            Notes: c.Notes,
            PostedByUserId: c.PostedByUserId), ct);

        var plates = c.Plates
            .Select(line => new GlassPlate(
                c.ProductId,
                c.WarehouseId,
                line.PlateNumber.Trim(),
                line.WidthMm,
                line.HeightMm,
                line.ThicknessMm,
                PlateKind.Fresh,
                receivedAt,
                c.StorageLocationId,
                c.LotId,
                parentPlateId: null,
                sourceReceiptMovementId: movement.Id,
                condition: line.Condition))
            .ToList();

        await _plates.AddRangeAsync(plates, ct);
        await _uow.SaveChangesAsync(ct);

        return new ReceiveGlassPlatesResultDto(movement.Id, plates.Count, totalAreaM2);
    }
}

public class MoveGlassPlateHandler : IRequestHandler<MoveGlassPlateCommand, GlassPlateDto>
{
    private readonly IGlassPlateRepository _plates;
    private readonly IAllocationService _allocation;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public MoveGlassPlateHandler(
        IGlassPlateRepository plates,
        IAllocationService allocation,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _plates = plates;
        _allocation = allocation;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<GlassPlateDto> Handle(MoveGlassPlateCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var plate = await _plates.GetByIdAsync(tenantId, c.PlateId, ct)
            ?? throw new GlassPlateNotFoundException(c.PlateId);

        if (c.WarehouseId != plate.WarehouseId)
        {
            var areaM2 = plate.RemainingAreaMm2 / 1_000_000m;
            if (areaM2 > 0m)
            {
                await _allocation.ApplyTransferAsync(plate.ProductId, plate.WarehouseId, c.WarehouseId, areaM2, "glass-plate-move", ct);
            }
        }

        plate.MoveTo(c.WarehouseId, c.StorageLocationId);
        await _uow.SaveChangesAsync(ct);
        return GlassPlateMapper.ToDto(plate);
    }
}
