using CoreAlign.Application.Common;
using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Commands;

public record ReceiveGlassPlateLine(
    string PlateNumber,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    PlateCondition Condition = PlateCondition.Good);

public record ReceiveGlassPlatesCommand(
    Guid ProductId,
    Guid WarehouseId,
    Guid? StorageLocationId,
    Guid? LotId,
    decimal UnitCostPerM2,
    IReadOnlyList<ReceiveGlassPlateLine> Plates,
    string? Notes,
    Guid PostedByUserId) : IRequest<ReceiveGlassPlatesResultDto>, ITransactionalRequest;

public record MoveGlassPlateCommand(
    Guid PlateId,
    Guid WarehouseId,
    Guid? StorageLocationId) : IRequest<GlassPlateDto>, ITransactionalRequest;

public record ScrapGlassPlateCommand(
    Guid? PlateId,
    Guid? ProductId,
    Guid? WarehouseId,
    GlassScrapMode Mode,
    decimal? AreaMm2,
    Guid ReasonCodeId,
    string? Notes,
    Guid PostedByUserId,
    Guid? WorkCenterId,
    Guid? OperatorId) : IRequest<GlassScrapResultDto>, ITransactionalRequest;

public record SetGlassPlateTrackingCommand(
    Guid ProductId,
    bool IsPlateTracked,
    decimal? MinRemnantAreaMm2,
    decimal? MinRemnantWidthMm,
    decimal? MinRemnantHeightMm,
    int? MinPlateCount,
    decimal? StandardWidthMm,
    decimal? StandardHeightMm) : IRequest<Guid>, ITransactionalRequest;

public record ConsumeGlassPlateCommand(
    Guid PlateId,
    decimal CutAreaMm2,
    int Pieces,
    decimal? CutWidthMm,
    decimal? CutHeightMm,
    decimal? RemnantWidthMm,
    decimal? RemnantHeightMm,
    string? RemnantPlateNumber,
    Guid? OrderLineId,
    Guid? JobId,
    Guid? WorkCenterId,
    Guid? OperatorId,
    Guid PostedByUserId) : IRequest<ConsumeGlassPlateResultDto>, ITransactionalRequest;
