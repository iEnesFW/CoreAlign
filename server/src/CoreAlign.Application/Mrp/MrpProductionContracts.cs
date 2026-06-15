using CoreAlign.Application.Common;
using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Mrp;

public record PlannedProductionOrderDto(
    Guid Id,
    Guid SourcePlanRunId,
    Guid ProductId,
    int LowLevelCode,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    decimal EstimatedUnitCost,
    LotSizingPolicy SourcePolicy,
    Guid? PeggingParentProductId,
    Guid? PeggingSourceOrderLineId,
    PlannedProductionOrderStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record ChangeImpactSupplyOrderDto(
    Guid ProductId,
    int LowLevelCode,
    OrderSinkKind SinkKind,
    decimal Quantity,
    DateTime DueDateUtc,
    DateTime ReleaseDateUtc,
    Guid? DirectParentProductId);

public record ChangeImpactResultDto(
    Guid PlanRunId,
    Guid SourceOrderLineId,
    IReadOnlyList<ChangeImpactSupplyOrderDto> DownstreamSupply);

public record ListPlannedProductionOrdersQuery(
    Guid? PlanRunId = null,
    Guid? ProductId = null,
    PlannedProductionOrderStatus? Status = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<PlannedProductionOrderDto>>;

public record GetProductionPeggingChainQuery(
    Guid PlanRunId,
    Guid ComponentProductId) : IRequest<IReadOnlyList<MrpPeggingDto>>;

public record GetChangeImpactQuery(
    Guid PlanRunId,
    Guid SourceOrderLineId) : IRequest<ChangeImpactResultDto>;

public record FirmPlannedProductionOrderCommand(
    Guid PlannedProductionOrderId,
    Guid OperationId,
    decimal? OverrideQuantity = null,
    DateTime? OverrideDueDateUtc = null) : IRequest<PlannedProductionOrderDto>, ITransactionalRequest;

public record ReleasePlannedProductionOrderCommand(
    Guid PlannedProductionOrderId,
    Guid OperationId) : IRequest<PlannedProductionOrderDto>, ITransactionalRequest;

public record CompletePlannedProductionOrderResultDto(
    Guid PlannedProductionOrderId,
    Guid ProductId,
    Guid WarehouseId,
    decimal ProducedQuantity,
    int ComponentsIssued,
    decimal UnitCost,
    decimal TotalCost,
    PlannedProductionOrderStatus Status,
    bool AlreadyCompleted);

public record CompletePlannedProductionOrderCommand(
    Guid PlannedProductionOrderId,
    Guid OperationId,
    Guid? WarehouseId = null) : IRequest<CompletePlannedProductionOrderResultDto>, ITransactionalRequest;
