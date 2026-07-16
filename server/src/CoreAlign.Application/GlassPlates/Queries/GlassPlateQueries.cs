using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Queries;

public record ListGlassPlatesQuery(
    Guid? ProductId,
    Guid? WarehouseId,
    Guid? StorageLocationId,
    GlassPlateStatus? Status,
    PlateKind? Kind,
    int Take = 200) : IRequest<IReadOnlyList<GlassPlateDto>>;

public record UsablePlatesForCutQuery(
    Guid ProductId,
    decimal WidthMm,
    decimal HeightMm,
    Guid? WarehouseId,
    int Take = 20) : IRequest<IReadOnlyList<GlassPlateDto>>;

public record GlassPlateWhereUsedQuery(Guid PlateId) : IRequest<IReadOnlyList<GlassPlateConsumptionDto>>;

public record LowStockPlatesQuery() : IRequest<IReadOnlyList<LowStockPlateDto>>;
