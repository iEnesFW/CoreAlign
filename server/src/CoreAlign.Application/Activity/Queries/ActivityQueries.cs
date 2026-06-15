using CoreAlign.Application.Activity.DTOs;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Activity.Queries;

public record ActivityLogFilter(
    Guid? UserId = null,
    string? Method = null,
    string? PathContains = null,
    int? StatusCode = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? StatusBucket = null,
    DateTime? DateFromUtc = null,
    DateTime? DateToUtc = null,
    string? EntityType = null,
    Guid? EntityId = null,
    string? Search = null);

public record GetActivityLogsQuery(
    int Page = 1,
    int PageSize = 30,
    ActivityLogFilter? Filter = null
) : IRequest<PagedResult<ActivityLogDto>>;

public record ExportActivityLogsCsvQuery(ActivityLogFilter? Filter = null) : IRequest<byte[]>;
