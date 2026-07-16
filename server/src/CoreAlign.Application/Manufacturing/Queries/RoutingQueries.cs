using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Queries;

public record GetProductionRoutingByIdQuery(Guid Id) : IRequest<ProductionRoutingDto>;

public record ListProductionRoutingsQuery(
    RoutingStatus? Status,
    int Take = 100) : IRequest<IReadOnlyList<ProductionRoutingSummaryDto>>;

public record GetWorkCenterOperatorByIdQuery(Guid Id) : IRequest<WorkCenterOperatorDto>;

public record ListWorkCenterOperatorsQuery(
    Guid? WorkCenterId,
    Guid? EmployeeId,
    int Take = 200) : IRequest<IReadOnlyList<WorkCenterOperatorDto>>;
