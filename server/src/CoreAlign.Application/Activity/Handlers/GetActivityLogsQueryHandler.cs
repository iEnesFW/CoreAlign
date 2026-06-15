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

        IReadOnlyList<ActivityLog> items;
        int total;
        if (HasActiveFilter(request.Filter))
        {
            (items, total) = await _repository.SearchAsync(ToQueryFilter(request.Filter), page, pageSize, cancellationToken);
        }
        else
        {
            (items, total) = await _repository.GetRecentAsync(page, pageSize, cancellationToken);
        }

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<ActivityLogDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static bool HasActiveFilter(ActivityLogFilter? filter)
    {
        if (filter is null) return false;
        return filter.UserId.HasValue
            || !string.IsNullOrWhiteSpace(filter.Method)
            || !string.IsNullOrWhiteSpace(filter.PathContains)
            || filter.StatusCode.HasValue
            || filter.FromUtc.HasValue
            || filter.ToUtc.HasValue
            || filter.DateFromUtc.HasValue
            || filter.DateToUtc.HasValue
            || !string.IsNullOrWhiteSpace(filter.StatusBucket)
            || !string.IsNullOrWhiteSpace(filter.EntityType)
            || filter.EntityId.HasValue
            || !string.IsNullOrWhiteSpace(filter.Search);
    }

    public static ActivityLogQueryFilter ToQueryFilter(ActivityLogFilter? filter) =>
        filter is null
            ? new ActivityLogQueryFilter()
            : new ActivityLogQueryFilter(
                UserId: filter.UserId,
                Method: filter.Method,
                PathContains: filter.PathContains,
                StatusCode: filter.StatusCode,
                FromUtc: filter.FromUtc ?? filter.DateFromUtc,
                ToUtc: filter.ToUtc ?? filter.DateToUtc,
                StatusBucket: filter.StatusBucket,
                EntityType: filter.EntityType,
                EntityId: filter.EntityId,
                Search: filter.Search);

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
