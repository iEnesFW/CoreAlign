using CoreAlign.Application.Common;
using CoreAlign.Application.Inventory.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Inventory.Commands;

public record AdjustStockCommand(
    Guid ProductId,
    Guid WarehouseId,
    decimal Delta,
    decimal? UnitCost,
    Guid? ReasonCodeId,
    Guid? LotId,
    string? Notes) : IRequest<StockMovementDto>, ITransactionalRequest;

public record ReceiveStockCommand(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    decimal UnitCost,
    Guid? LotId,
    string? SerialNumber,
    Guid? ReasonCodeId,
    string? Reference,
    string? Notes) : IRequest<StockMovementDto>, ITransactionalRequest;

public record IssueStockCommand(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    Guid? LotId,
    string? SerialNumber,
    Guid? ReasonCodeId,
    string? Reference,
    string? Notes) : IRequest<StockMovementDto>, ITransactionalRequest;

public record CreateLotCommand(
    Guid ProductId,
    string LotNumber,
    DateTime? ManufactureDate,
    DateTime? ExpiryDate,
    string? SupplierLotRef,
    string? CountryOfOrigin,
    string? Notes) : IRequest<LotDto>, ITransactionalRequest;

public record UpdateLotCommand(
    Guid Id,
    string LotNumber,
    DateTime? ManufactureDate,
    DateTime? ExpiryDate,
    string? SupplierLotRef,
    string? CountryOfOrigin,
    string? Notes,
    bool IsBlocked,
    string? BlockReason) : IRequest<LotDto>, ITransactionalRequest;

public record CreateStockReasonCodeCommand(
    string Code,
    string Name,
    StockReasonCategory Category,
    bool AffectsCost = true,
    string? Description = null) : IRequest<StockReasonCodeDto>, ITransactionalRequest;

public record UpdateStockReasonCodeCommand(
    Guid Id,
    string Code,
    string Name,
    StockReasonCategory Category,
    bool AffectsCost,
    string? Description,
    bool IsActive) : IRequest<StockReasonCodeDto>, ITransactionalRequest;
