using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.DTOs;
using CoreAlign.Application.Inventory.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Inventory.Handlers;

public class AdjustStockHandler : IRequestHandler<AdjustStockCommand, StockMovementDto>
{
    private readonly IAllocationService _allocation;
    private readonly IUnitOfWork _uow;

    public AdjustStockHandler(IAllocationService allocation, IUnitOfWork uow)
    {
        _allocation = allocation;
        _uow = uow;
    }

    public async Task<StockMovementDto> Handle(AdjustStockCommand c, CancellationToken ct)
    {
        var movement = await _allocation.AdjustAsync(new StockAdjustmentRequest(
            ProductId: c.ProductId,
            WarehouseId: c.WarehouseId,
            Delta: c.Delta,
            UnitCost: c.UnitCost,
            SourceDocumentType: StockSourceDocumentType.Adjustment,
            SourceDocumentId: null,
            ReasonCodeId: c.ReasonCodeId,
            Notes: c.Notes,
            LotId: c.LotId), ct);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(movement);
    }
}

public class ReceiveStockHandler : IRequestHandler<ReceiveStockCommand, StockMovementDto>
{
    private readonly IAllocationService _allocation;
    private readonly IUnitOfWork _uow;

    public ReceiveStockHandler(IAllocationService allocation, IUnitOfWork uow)
    {
        _allocation = allocation;
        _uow = uow;
    }

    public async Task<StockMovementDto> Handle(ReceiveStockCommand c, CancellationToken ct)
    {
        var movement = await _allocation.ApplyReceiptAsync(new StockReceiptRequest(
            ProductId: c.ProductId,
            WarehouseId: c.WarehouseId,
            Quantity: c.Quantity,
            UnitCost: c.UnitCost,
            SourceDocumentType: StockSourceDocumentType.Adjustment,
            SourceDocumentId: null,
            SourceLineId: null,
            SourceReference: c.Reference,
            LotId: c.LotId,
            SerialNumber: c.SerialNumber,
            ReasonCodeId: c.ReasonCodeId,
            Notes: c.Notes), ct);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(movement);
    }
}

public class IssueStockHandler : IRequestHandler<IssueStockCommand, StockMovementDto>
{
    private readonly IAllocationService _allocation;
    private readonly IUnitOfWork _uow;

    public IssueStockHandler(IAllocationService allocation, IUnitOfWork uow)
    {
        _allocation = allocation;
        _uow = uow;
    }

    public async Task<StockMovementDto> Handle(IssueStockCommand c, CancellationToken ct)
    {
        var movement = await _allocation.ApplyIssueAsync(new StockIssueRequest(
            ProductId: c.ProductId,
            WarehouseId: c.WarehouseId,
            Quantity: c.Quantity,
            SourceDocumentType: StockSourceDocumentType.Adjustment,
            SourceDocumentId: null,
            SourceLineId: null,
            SourceReference: c.Reference,
            LotId: c.LotId,
            SerialNumber: c.SerialNumber,
            ReasonCodeId: c.ReasonCodeId,
            Notes: c.Notes), ct);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(movement);
    }
}

public class ApplyStockTransferHandler : IRequestHandler<ApplyStockTransferCommand, StockTransferResultDto>
{
    private readonly IAllocationService _allocation;
    private readonly IUnitOfWork _uow;

    public ApplyStockTransferHandler(IAllocationService allocation, IUnitOfWork uow)
    {
        _allocation = allocation;
        _uow = uow;
    }

    public async Task<StockTransferResultDto> Handle(ApplyStockTransferCommand c, CancellationToken ct)
    {
        var result = await _allocation.ApplyTransferAsync(
            c.ProductId,
            c.FromWarehouseId,
            c.ToWarehouseId,
            c.Quantity,
            c.Reference,
            ct);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(result);
    }
}

public class CreateLotHandler : IRequestHandler<CreateLotCommand, LotDto>
{
    private readonly ILotRepository _lots;
    private readonly IUnitOfWork _uow;

    public CreateLotHandler(ILotRepository lots, IUnitOfWork uow)
    {
        _lots = lots;
        _uow = uow;
    }

    public async Task<LotDto> Handle(CreateLotCommand c, CancellationToken ct)
    {
        var lot = new Lot(c.ProductId, c.LotNumber, c.ManufactureDate, c.ExpiryDate, c.SupplierLotRef);
        lot.Update(c.LotNumber, c.ManufactureDate, c.ExpiryDate, c.SupplierLotRef, c.CountryOfOrigin, c.Notes);
        await _lots.AddAsync(lot, ct);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(lot);
    }
}

public class UpdateLotHandler : IRequestHandler<UpdateLotCommand, LotDto>
{
    private readonly ILotRepository _lots;
    private readonly IUnitOfWork _uow;

    public UpdateLotHandler(ILotRepository lots, IUnitOfWork uow)
    {
        _lots = lots;
        _uow = uow;
    }

    public async Task<LotDto> Handle(UpdateLotCommand c, CancellationToken ct)
    {
        var lot = await _lots.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException("Lot not found");
        lot.Update(c.LotNumber, c.ManufactureDate, c.ExpiryDate, c.SupplierLotRef, c.CountryOfOrigin, c.Notes);
        if (c.IsBlocked) lot.Block(c.BlockReason ?? "Blocked");
        else lot.Unblock();
        _lots.Update(lot);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(lot);
    }
}

public class CreateStockReasonCodeHandler : IRequestHandler<CreateStockReasonCodeCommand, StockReasonCodeDto>
{
    private readonly IStockReasonCodeRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateStockReasonCodeHandler(IStockReasonCodeRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<StockReasonCodeDto> Handle(CreateStockReasonCodeCommand c, CancellationToken ct)
    {
        var r = new StockReasonCode(c.Code, c.Name, c.Category, c.AffectsCost, c.Description);
        await _repo.AddAsync(r, ct);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(r);
    }
}

public class UpdateStockReasonCodeHandler : IRequestHandler<UpdateStockReasonCodeCommand, StockReasonCodeDto>
{
    private readonly IStockReasonCodeRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateStockReasonCodeHandler(IStockReasonCodeRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<StockReasonCodeDto> Handle(UpdateStockReasonCodeCommand c, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(c.Id, ct) ?? throw new KeyNotFoundException("StockReasonCode not found");
        r.Update(c.Code, c.Name, c.Category, c.AffectsCost, c.Description, c.IsActive);
        _repo.Update(r);
        await _uow.SaveChangesAsync(ct);
        return InventoryMapper.ToDto(r);
    }
}
