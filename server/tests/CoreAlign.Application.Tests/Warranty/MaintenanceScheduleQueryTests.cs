using CoreAlign.Application.Warranty;
using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Warranty;

public class MaintenanceScheduleQueryTests
{
    private readonly IMaintenanceScheduleRepository _repo = Substitute.For<IMaintenanceScheduleRepository>();
    private readonly ListMaintenanceSchedulesDueHandler _sut;

    public MaintenanceScheduleQueryTests()
    {
        _sut = new ListMaintenanceSchedulesDueHandler(_repo);
    }

    [Fact]
    public async Task Handle_returns_schedules_filtered_by_due_date()
    {
        var today = new DateTime(2026, 06, 04, 0, 0, 0, DateTimeKind.Utc);
        var contractId = Guid.NewGuid();
        var dueYesterday = BuildSchedule(contractId, today.AddDays(-1));
        var dueToday = BuildSchedule(contractId, today);

        _repo.ListDueAsync(today, Arg.Any<CancellationToken>()).Returns(new List<MaintenanceSchedule> { dueYesterday, dueToday });

        var result = await _sut.Handle(new ListMaintenanceSchedulesDueQuery(today), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.NextDueDate <= today);
        await _repo.Received(1).ListDueAsync(today, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_empty_when_repository_returns_no_due_schedules()
    {
        var today = DateTime.UtcNow;
        _repo.ListDueAsync(today, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MaintenanceSchedule>());

        var result = await _sut.Handle(new ListMaintenanceSchedulesDueQuery(today), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static MaintenanceSchedule BuildSchedule(Guid contractId, DateTime nextDueDate)
    {
        var schedule = new MaintenanceSchedule(
            warrantyContractId: contractId,
            type: MaintenanceScheduleType.PreventiveAnnual,
            nextDueDate: nextDueDate,
            recurrencePattern: "0 0 1 1 *");
        schedule.Id = Guid.NewGuid();
        return schedule;
    }
}
