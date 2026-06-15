using CoreAlign.Application.Common;
using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Returns.Queries;

public record GetReturnRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CustomerId = null,
    Guid? OrderId = null,
    ReturnRequestStatus? Status = null) : IRequest<PagedResult<ReturnRequestSummaryDto>>;

public record GetReturnRequestByIdQuery(Guid Id) : IRequest<ReturnRequestDto>;

public record GetReturnRequestsByOrderQuery(Guid OrderId) : IRequest<List<ReturnRequestSummaryDto>>;
