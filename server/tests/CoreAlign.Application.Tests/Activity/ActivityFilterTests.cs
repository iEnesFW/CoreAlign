using CoreAlign.Application.Activity.Handlers;
using CoreAlign.Application.Activity.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Activity;

public class ActivityFilterTests
{
    private readonly IActivityLogRepository _repo = Substitute.For<IActivityLogRepository>();

    [Fact]
    public async Task Filtered_query_passes_filter_to_search_method()
    {
        var filter = new ActivityLogFilter(
            UserId: Guid.NewGuid(),
            Method: "POST",
            StatusBucket: "5xx",
            DateFromUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DateToUtc: new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            EntityType: "customers",
            EntityId: null,
            Search: null);

        _repo.SearchAsync(Arg.Any<ActivityLogQueryFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ActivityLog>)new List<ActivityLog>(), 0));

        var handler = new GetActivityLogsQueryHandler(_repo);
        await handler.Handle(new GetActivityLogsQuery(1, 30, filter), default);

        await _repo.Received(1).SearchAsync(
            Arg.Is<ActivityLogQueryFilter>(f =>
                f.Method == "POST"
                && f.StatusBucket == "5xx"
                && f.EntityType == "customers"
                && f.UserId == filter.UserId),
            1,
            30,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unfiltered_query_falls_back_to_get_recent()
    {
        _repo.GetRecentAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ActivityLog>)new List<ActivityLog>(), 0));

        var handler = new GetActivityLogsQueryHandler(_repo);
        await handler.Handle(new GetActivityLogsQuery(1, 10, null), default);

        await _repo.Received(1).GetRecentAsync(1, 10, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SearchAsync(Arg.Any<ActivityLogQueryFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Page_and_page_size_are_clamped_to_safe_bounds()
    {
        _repo.GetRecentAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ActivityLog>)new List<ActivityLog>(), 0));

        var handler = new GetActivityLogsQueryHandler(_repo);
        await handler.Handle(new GetActivityLogsQuery(-5, 5000), default);

        await _repo.Received(1).GetRecentAsync(1, 200, Arg.Any<CancellationToken>());
    }
}
