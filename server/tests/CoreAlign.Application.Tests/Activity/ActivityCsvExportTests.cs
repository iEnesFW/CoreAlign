using CoreAlign.Application.Activity.Handlers;
using CoreAlign.Application.Activity.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Activity;

public class ActivityCsvExportTests
{
    private readonly IActivityLogRepository _repo = Substitute.For<IActivityLogRepository>();

    [Fact]
    public async Task Exports_header_and_rows_with_correct_columns()
    {
        var now = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);
        var log = new ActivityLog
        {
            Method = "GET",
            Path = "/api/v1/customers/123",
            StatusCode = 200,
            DurationMs = 17,
            IpAddress = "127.0.0.1",
            TraceId = "trace-1",
            CreatedAtUtc = now
        };
        _repo.StreamAsync(Arg.Any<ActivityLogQueryFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ActivityLog> { log });

        var handler = new ExportActivityLogsCsvQueryHandler(_repo);
        var bytes = await handler.Handle(new ExportActivityLogsCsvQuery(new ActivityLogFilter()), default);

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        text.Should().Contain("CreatedAtUtc,UserId,Method,Path,StatusCode,DurationMs,IpAddress,TraceId");
        text.Should().Contain("/api/v1/customers/123");
        text.Should().Contain("trace-1");
    }

    [Fact]
    public void CsvEscape_quotes_and_doubles_inner_quotes()
    {
        ExportActivityLogsCsvQueryHandler.CsvEscape("hello").Should().Be("hello");
        ExportActivityLogsCsvQueryHandler.CsvEscape("a,b").Should().Be("\"a,b\"");
        ExportActivityLogsCsvQueryHandler.CsvEscape("she said \"hi\"").Should().Be("\"she said \"\"hi\"\"\"");
        ExportActivityLogsCsvQueryHandler.CsvEscape("multi\nline").Should().Be("\"multi\nline\"");
    }
}
