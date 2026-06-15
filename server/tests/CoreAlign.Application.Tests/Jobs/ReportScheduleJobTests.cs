using CoreAlign.Application.Common.Email;
using CoreAlign.Application.Jobs;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Jobs;

public class ReportScheduleJobTests
{
    private readonly IReportScheduleRepository _repository = Substitute.For<IReportScheduleRepository>();
    private readonly IEmailQueuedOutbox _email = Substitute.For<IEmailQueuedOutbox>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public ReportScheduleJobTests()
    {
        _tenant.PushScope(Arg.Any<Guid>()).Returns(_ => Substitute.For<IDisposable>());
        _uow.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Substitute.For<IUnitOfWorkTransaction>());
    }

    [Fact]
    public async Task No_ops_when_no_schedules_due()
    {
        _repository.GetDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReportSchedule>());
        var sut = new ReportScheduleJob(_repository, _email, _tenant, _uow, NullLogger<ReportScheduleJob>.Instance);

        await sut.RunAsync();

        await _email.DidNotReceive().EnqueueAsync(Arg.Any<EmailQueuedPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Enqueues_one_email_per_recipient_and_advances_next_run()
    {
        var due = NewSchedule(new[] { "ops@example.com", "cfo@example.com" });
        _repository.GetDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { due });

        var sut = new ReportScheduleJob(_repository, _email, _tenant, _uow, NullLogger<ReportScheduleJob>.Instance);
        await sut.RunAsync();

        await _email.Received(2).EnqueueAsync(Arg.Any<EmailQueuedPayload>(), Arg.Any<CancellationToken>());
        due.LastRunStatus.Should().Be("Ok");
        due.NextRunAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Records_failure_when_recipient_outbox_throws()
    {
        var due = NewSchedule(new[] { "ops@example.com" });
        _repository.GetDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { due });
        _repository.GetByIdAsync(due.Id, Arg.Any<CancellationToken>()).Returns(due);
        _email.EnqueueAsync(Arg.Any<EmailQueuedPayload>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        var sut = new ReportScheduleJob(_repository, _email, _tenant, _uow, NullLogger<ReportScheduleJob>.Instance);
        await sut.RunAsync();

        due.LastRunStatus.Should().Be("Failed");
        due.LastRunError.Should().Contain("boom");
        due.NextRunAtUtc.Should().BeAfter(DateTime.UtcNow);
        _uow.Received().ClearChangeTracker();
    }

    [Fact]
    public async Task Skips_schedule_when_recently_succeeded_within_dedupe_window()
    {
        var due = NewSchedule(new[] { "ops@example.com" });
        var recent = DateTime.UtcNow.AddMinutes(-1);
        due.RecordRun(recent, "Ok", null, DateTime.UtcNow.AddDays(1));
        _repository.GetDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { due });

        var sut = new ReportScheduleJob(_repository, _email, _tenant, _uow, NullLogger<ReportScheduleJob>.Instance);
        await sut.RunAsync();

        await _email.DidNotReceive().EnqueueAsync(Arg.Any<EmailQueuedPayload>(), Arg.Any<CancellationToken>());
    }

    private static ReportSchedule NewSchedule(string[] recipients)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(recipients);
        return new ReportSchedule(
            name: "Daily",
            reportKey: "inventory-stock-on-hand",
            customReportDefinitionId: null,
            frequency: ReportFrequency.Daily,
            cronExpression: null,
            recipientsJson: json,
            format: ReportDeliveryFormat.Pdf,
            filtersJson: "{}",
            nextRunAtUtc: DateTime.UtcNow.AddMinutes(-1));
    }
}
