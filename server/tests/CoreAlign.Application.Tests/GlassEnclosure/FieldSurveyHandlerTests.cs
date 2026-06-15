using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class FieldSurveyHandlerTests
{
    private readonly IFieldSurveyRepository _surveyRepo = Substitute.For<IFieldSurveyRepository>();
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IFieldSurveyApplier _applier = Substitute.For<IFieldSurveyApplier>();
    private readonly IProjectRecomputeService _recompute = Substitute.For<IProjectRecomputeService>();
    private readonly IBomStaleSignal _bomStaleSignal = Substitute.For<IBomStaleSignal>();

    [Fact]
    public async Task Apply_rejects_when_already_applied()
    {
        var survey = BuildSurvey(FieldSurveyStatus.Approved);
        survey.MarkApplied();
        _surveyRepo.GetByIdAsync(survey.Id, Arg.Any<CancellationToken>()).Returns(survey);
        var sut = new ApplyFieldSurveyCommandHandler(_surveyRepo, _projectRepo, _applier, _recompute, _bomStaleSignal);

        var act = async () => await sut.Handle(new ApplyFieldSurveyCommand(survey.Id), default);

        await act.Should().ThrowAsync<GlassFieldSurveyNotApplicableException>();
        await _applier.DidNotReceive().ApplyAsync(Arg.Any<GlassProject>(), Arg.Any<FieldSurvey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_rejects_when_status_not_approved_or_submitted()
    {
        var survey = BuildSurvey(FieldSurveyStatus.Rejected);
        _surveyRepo.GetByIdAsync(survey.Id, Arg.Any<CancellationToken>()).Returns(survey);
        var sut = new ApplyFieldSurveyCommandHandler(_surveyRepo, _projectRepo, _applier, _recompute, _bomStaleSignal);

        var act = async () => await sut.Handle(new ApplyFieldSurveyCommand(survey.Id), default);

        await act.Should().ThrowAsync<GlassFieldSurveyNotApplicableException>();
    }

    [Fact]
    public async Task Apply_marks_applied_on_success()
    {
        var survey = BuildSurvey(FieldSurveyStatus.Approved);
        var project = BuildProject(survey.TenantId);
        _surveyRepo.GetByIdAsync(survey.Id, Arg.Any<CancellationToken>()).Returns(survey);
        _projectRepo.GetByIdWithRunsAsync(survey.ProjectId, Arg.Any<CancellationToken>()).Returns(project);
        _applier.ApplyAsync(project, survey, Arg.Any<CancellationToken>())
            .Returns(new FieldSurveyApplyResult(project.Id, survey.Id, 0, 0m, 5, 5));
        var sut = new ApplyFieldSurveyCommandHandler(_surveyRepo, _projectRepo, _applier, _recompute, _bomStaleSignal);

        await sut.Handle(new ApplyFieldSurveyCommand(survey.Id), default);

        survey.AppliedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Apply_rejects_when_project_tenant_mismatch()
    {
        var survey = BuildSurvey(FieldSurveyStatus.Approved);
        var project = BuildProject(Guid.NewGuid());
        _surveyRepo.GetByIdAsync(survey.Id, Arg.Any<CancellationToken>()).Returns(survey);
        _projectRepo.GetByIdWithRunsAsync(survey.ProjectId, Arg.Any<CancellationToken>()).Returns(project);
        var sut = new ApplyFieldSurveyCommandHandler(_surveyRepo, _projectRepo, _applier, _recompute, _bomStaleSignal);

        var act = async () => await sut.Handle(new ApplyFieldSurveyCommand(survey.Id), default);

        await act.Should().ThrowAsync<GlassFieldSurveyNotApplicableException>();
    }

    [Fact]
    public async Task Delete_rejects_non_in_progress()
    {
        var survey = BuildSurvey(FieldSurveyStatus.Submitted);
        _surveyRepo.GetByIdAsync(survey.Id, Arg.Any<CancellationToken>()).Returns(survey);
        var sut = new DeleteFieldSurveyCommandHandler(_surveyRepo);

        var act = async () => await sut.Handle(new DeleteFieldSurveyCommand(survey.Id), default);

        await act.Should().ThrowAsync<GlassFieldSurveyNotApplicableException>();
        _surveyRepo.DidNotReceive().Remove(Arg.Any<FieldSurvey>());
    }

    [Fact]
    public async Task Delete_allows_in_progress()
    {
        var survey = BuildSurvey(FieldSurveyStatus.InProgress);
        _surveyRepo.GetByIdAsync(survey.Id, Arg.Any<CancellationToken>()).Returns(survey);
        var sut = new DeleteFieldSurveyCommandHandler(_surveyRepo);

        await sut.Handle(new DeleteFieldSurveyCommand(survey.Id), default);

        _surveyRepo.Received(1).Remove(survey);
    }

    private static FieldSurvey BuildSurvey(FieldSurveyStatus status)
    {
        var tenantId = Guid.NewGuid();
        var survey = new FieldSurvey(Guid.NewGuid(), Guid.NewGuid())
        {
            TenantId = tenantId,
        };
        if (status == FieldSurveyStatus.Submitted || status == FieldSurveyStatus.Approved || status == FieldSurveyStatus.Rejected)
        {
            survey.Submit();
        }
        if (status == FieldSurveyStatus.Approved)
        {
            survey.Approve();
        }
        if (status == FieldSurveyStatus.Rejected)
        {
            survey.Reject(null);
        }
        return survey;
    }

    private static GlassProject BuildProject(Guid tenantId)
    {
        return new GlassProject("PROJ-1", Guid.NewGuid(), "Test", Guid.NewGuid())
        {
            TenantId = tenantId,
        };
    }
}
