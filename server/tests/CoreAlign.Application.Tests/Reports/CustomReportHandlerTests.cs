using CoreAlign.Application.B2B;
using CoreAlign.Application.Reports.Custom;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Reports;

public class CustomReportHandlerTests
{
    private readonly IReportDefinitionRepository _repository = Substitute.For<IReportDefinitionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserAccessor _user = Substitute.For<ICurrentUserAccessor>();
    private readonly ICustomReportExecutor _executor = Substitute.For<ICustomReportExecutor>();

    [Fact]
    public async Task SaveCommand_validates_definition_and_persists_entity()
    {
        _user.UserId.Returns(Guid.NewGuid());
        var sut = new SaveCustomReportCommandHandler(_repository, _uow, _user);
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName" },
            new[] { new CustomReportMeasureDto("Total", "Sum") });

        var summary = await sut.Handle(new SaveCustomReportCommand(
            new SaveCustomReportRequestDto("My report", "Desc", def)), CancellationToken.None);

        summary.Name.Should().Be("My report");
        summary.EntityType.Should().Be("Invoice");
        await _repository.Received(1).AddAsync(Arg.Any<ReportDefinition>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveCommand_throws_for_unknown_field()
    {
        var sut = new SaveCustomReportCommandHandler(_repository, _uow, _user);
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "WHERE 1=1 OR 1=1" },
            Array.Empty<CustomReportMeasureDto>());

        Func<Task> act = () => sut.Handle(new SaveCustomReportCommand(
            new SaveCustomReportRequestDto("My report", null, def)), CancellationToken.None);

        await act.Should().ThrowAsync<CustomReportValidationException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<ReportDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListQuery_maps_entities_to_summary_dtos()
    {
        var entity = new ReportDefinition(
            name: "X",
            entityType: ReportEntityType.Order,
            dimensionsJson: "[]",
            measuresJson: "[]",
            filtersJson: "[]",
            sortByJson: null,
            limit: null);
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { entity });

        var sut = new ListCustomReportsQueryHandler(_repository);
        var result = await sut.Handle(new ListCustomReportsQuery(), CancellationToken.None);
        result.Should().ContainSingle().Which.EntityType.Should().Be("Order");
    }

    [Fact]
    public async Task DeleteCommand_returns_true_when_entity_found()
    {
        var entity = new ReportDefinition(
            name: "X",
            entityType: ReportEntityType.Order,
            dimensionsJson: "[]",
            measuresJson: "[]",
            filtersJson: "[]",
            sortByJson: null,
            limit: null);
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var sut = new DeleteCustomReportCommandHandler(_repository, _uow);
        var result = await sut.Handle(new DeleteCustomReportCommand(entity.Id), CancellationToken.None);
        result.Should().BeTrue();
        _repository.Received(1).Remove(entity);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCommand_returns_false_when_entity_missing()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ReportDefinition?)null);
        var sut = new DeleteCustomReportCommandHandler(_repository, _uow);
        var result = await sut.Handle(new DeleteCustomReportCommand(id), CancellationToken.None);
        result.Should().BeFalse();
        _repository.DidNotReceive().Remove(Arg.Any<ReportDefinition>());
    }

    [Fact]
    public async Task PreviewQuery_delegates_to_executor()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName" },
            new[] { new CustomReportMeasureDto("Total", "Sum") });
        var preview = new CustomReportPreviewDto(
            new[] { "CustomerName", "Sum_Total" },
            Array.Empty<CustomReportPreviewRowDto>(),
            0,
            false);
        _executor.ExecuteAsync(def, Arg.Any<CancellationToken>()).Returns(preview);

        var sut = new PreviewCustomReportQueryHandler(_executor);
        var result = await sut.Handle(new PreviewCustomReportQuery(def), CancellationToken.None);
        result.Should().BeSameAs(preview);
    }
}
