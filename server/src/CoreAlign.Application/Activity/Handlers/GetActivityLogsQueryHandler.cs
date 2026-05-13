using CoreAlign.Application.Activity.DTOs;
using CoreAlign.Application.Activity.Queries;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Activity.Handlers;

public class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, PagedResult<ActivityLogDto>>
{
    private readonly IActivityLogRepository _repository;

    public GetActivityLogsQueryHandler(IActivityLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<ActivityLogDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _repository.GetRecentAsync(page, pageSize, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<ActivityLogDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static ActivityLogDto MapToDto(ActivityLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        Method = log.Method,
        Path = log.Path,
        StatusCode = log.StatusCode,
        DurationMs = log.DurationMs,
        IpAddress = log.IpAddress,
        UserAgent = log.UserAgent,
        TraceId = log.TraceId,
        CreatedAtUtc = log.CreatedAtUtc
    };
}
