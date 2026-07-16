using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Queries;

public record ListProductionJobsQuery(
    ProductionJobStatus? Status,
    Guid? ProductId,
    int Take) : IRequest<IReadOnlyList<ProductionJobListDto>>;

public record GetProductionJobByIdQuery(Guid Id) : IRequest<ProductionJobDetailDto>;
