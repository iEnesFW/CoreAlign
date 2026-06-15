using CoreAlign.Application.B2B;
using CoreAlign.Application.Reports.Schedules;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Reports;

public class ScheduleHandlerTests
{
    private readonly IReportScheduleRepository _repository = Substitute.For<IReportScheduleRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserAccessor _user = Substitute.For<ICurrentUserAccessor>();

    [Fact]
    public async Task Create_throws_when_recipients_empty()
    {
        _user.UserId.Returns(Guid.NewGuid());
        var sut = new CreateReportScheduleCommandHandler(_repository, _uow, _user);
        var payload = new CreateReportScheduleRequestDto(
            "Daily ops",
            "inventory-stock-on-hand",
            null,
            ReportFrequency.Daily,
            null,
            Array.Empty<string>(),
            ReportDeliveryFormat.Pdf,
            null,
            null);

        Func<Task> act = () => sut.Handle(new CreateReportScheduleCommand(payload), CancellationToken.None);
        await act.Should().ThrowAsync<ScheduleValidationException>();
    }

    [Fact]
    public async Task Create_persists_and_returns_dto_with_recipients()
    {
        _user.UserId.Returns(Guid.NewGuid());
        var sut = new CreateReportScheduleCommandHandler(_repository, _uow, _user);
        var payload = new CreateReportScheduleRequestDto(
            "Daily ops",
            "inventory-stock-on-hand",
            null,
            ReportFrequency.Daily,
            null,
            new[] { "ops@example.com" },
            ReportDeliveryFormat.Pdf,
            null,
            DateTime.UtcNow.AddDays(1));

        var result = await sut.Handle(new CreateReportScheduleCommand(payload), CancellationToken.None);

        result.Recipients.Should().ContainSingle().Which.Should().Be("ops@example.com");
        result.IsActive.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<ReportSchedule>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_throws_when_recipients_empty()
    {
        var entity = NewSchedule();
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var sut = new UpdateReportScheduleCommandHandler(_repository, _uow);
        var payload = new UpdateReportScheduleRequestDto(
            "Daily ops",
            "inventory-stock-on-hand",
            null,
            ReportFrequency.Daily,
            null,
            Array.Empty<string>(),
            ReportDeliveryFormat.Pdf,
            null,
            null);

        Func<Task> act = () => sut.Handle(new UpdateReportScheduleCommand(entity.Id, payload), CancellationToken.None);
        await act.Should().ThrowAsync<ScheduleValidationException>();
    }

    [Fact]
    public async Task Update_returns_null_when_entity_missing()
    {
        var sut = new UpdateReportScheduleCommandHandler(_repository, _uow);
        var payload = new UpdateReportScheduleRequestDto(
            "Daily ops",
            "inventory-stock-on-hand",
            null,
            ReportFrequency.Daily,
            null,
            new[] { "ops@example.com" },
            ReportDeliveryFormat.Pdf,
            null,
            null);
        var result = await sut.Handle(new UpdateReportScheduleCommand(Guid.NewGuid(), payload), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_toggles_active_when_payload_requests()
    {
        var entity = NewSchedule();
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var sut = new UpdateReportScheduleCommandHandler(_repository, _uow);
        var payload = new UpdateReportScheduleRequestDto(
            "Daily ops",
            "inventory-stock-on-hand",
            null,
            ReportFrequency.Daily,
            null,
            new[] { "ops@example.com" },
            ReportDeliveryFormat.Pdf,
            null,
            false);

        var result = await sut.Handle(new UpdateReportScheduleCommand(entity.Id, payload), CancellationToken.None);
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_returns_false_when_entity_missing()
    {
        var sut = new DeleteReportScheduleCommandHandler(_repository, _uow);
        var result = await sut.Handle(new DeleteReportScheduleCommand(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task List_maps_entities_through_mapper()
    {
        var entity = NewSchedule();
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { entity });
        var sut = new ListReportSchedulesQueryHandler(_repository);
        var result = await sut.Handle(new ListReportSchedulesQuery(), CancellationToken.None);
        result.Should().ContainSingle().Which.Name.Should().Be("Daily ops");
    }

    private static ReportSchedule NewSchedule() =>
        new ReportSchedule(
            name: "Daily ops",
            reportKey: "inventory-stock-on-hand",
            customReportDefinitionId: null,
            frequency: ReportFrequency.Daily,
            cronExpression: null,
            recipientsJson: "[\"ops@example.com\"]",
            format: ReportDeliveryFormat.Pdf,
            filtersJson: "{}",
            nextRunAtUtc: DateTime.UtcNow.AddHours(1));
}
