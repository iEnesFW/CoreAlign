using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Inventory.StockCounts;

public record StockCountLineDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid? LotId,
    string? LotNumber,
    string? BinLocation,
    decimal ExpectedQuantity,
    decimal? CountedQuantity,
    decimal VarianceQuantity,
    decimal SnapshotUnitCost,
    decimal VarianceCost,
    DateTime? CountedAtUtc,
    Guid? CountedByUserId,
    string? LineNotes);

public record StockCountDto(
    Guid Id,
    string CountNumber,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    StockCountStatus Status,
    DateTime PlannedAtUtc,
    DateTime? CountingStartedAtUtc,
    DateTime? ReconciledAtUtc,
    DateTime? PostedAtUtc,
    Guid? PlannedByUserId,
    Guid? PostedByUserId,
    string? Notes,
    decimal TotalVarianceQuantity,
    decimal TotalVarianceCost,
    IReadOnlyList<StockCountLineDto> Lines,
    DateTime CreatedAtUtc);

public record PlanStockCountCommand(
    Guid WarehouseId,
    string? CountNumber = null,
    string? Notes = null) : IRequest<StockCountDto>, ITransactionalRequest;

public record StartStockCountCommand(Guid Id) : IRequest<StockCountDto>, ITransactionalRequest;

public record RecordCountLineInput(Guid LineId, decimal CountedQuantity, string? LineNotes = null);

public record RecordCountCommand(
    Guid Id,
    List<RecordCountLineInput> Lines) : IRequest<StockCountDto>, ITransactionalRequest;

public record ReconcileStockCountCommand(Guid Id, string? Notes = null) : IRequest<StockCountDto>, ITransactionalRequest;

public record PostStockCountCommand(Guid Id) : IRequest<StockCountDto>, ITransactionalRequest;

public record CancelStockCountCommand(Guid Id) : IRequest<StockCountDto>, ITransactionalRequest;

public record GetStockCountByIdQuery(Guid Id) : IRequest<StockCountDto?>;

public record SearchStockCountsQuery(
    Guid? WarehouseId = null,
    StockCountStatus? Status = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<StockCountDto>>;
