using CoreAlign.Application.Activity.DTOs;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Activity.Queries;

public record GetActivityLogsQuery(
    int Page = 1,
    int PageSize = 30
) : IRequest<PagedResult<ActivityLogDto>>;
