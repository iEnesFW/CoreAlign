using CoreAlign.Application.Manufacturing.DTOs;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Queries;

public record GetWorkCenterByIdQuery(Guid Id) : IRequest<WorkCenterDto>;

public record ListWorkCentersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<WorkCenterDto>>;
