using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

internal static class FieldSurveyMapper
{
    public static FieldSurveyDto ToDto(FieldSurvey s) => new(
        s.Id, s.ProjectId, s.SurveyedByUserId, s.SurveyedAtUtc,
        s.GpsLat, s.GpsLng, s.FloorNumber, s.BuildingHeightM,
        s.SlopeTopMm, s.SlopeBottomMm, s.SlopeLeftMm, s.SlopeRightMm,
        s.RawMeasurementsJson, s.ObstaclesJson, s.PhotoUrlsJson, s.AnnotatedPhotoUrlsJson,
        s.Status, s.AppliedAtUtc, s.Notes);
}

public class CreateFieldSurveyCommandHandler : IRequestHandler<CreateFieldSurveyCommand, FieldSurveyDto>
{
    private readonly IFieldSurveyRepository _repo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateFieldSurveyCommandHandler(
        IFieldSurveyRepository repo,
        IGlassProjectRepository projectRepo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
    }

    public async Task<FieldSurveyDto> Handle(CreateFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.Data.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var survey = new FieldSurvey(
            project.Id,
            _currentUser.UserId ?? Guid.Empty,
            request.Data.GpsLat,
            request.Data.GpsLng,
            request.Data.FloorNumber,
            request.Data.BuildingHeightM,
            request.Data.Notes);
        await _repo.AddAsync(survey, cancellationToken);
        return FieldSurveyMapper.ToDto(survey);
    }
}

public class UpdateFieldSurveyCommandHandler : IRequestHandler<UpdateFieldSurveyCommand, FieldSurveyDto>
{
    private readonly IFieldSurveyRepository _repo;
    public UpdateFieldSurveyCommandHandler(IFieldSurveyRepository repo) => _repo = repo;

    public async Task<FieldSurveyDto> Handle(UpdateFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (survey.Status != FieldSurveyStatus.InProgress)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        survey.UpdateMeasurements(
            request.Data.SlopeTopMm, request.Data.SlopeBottomMm,
            request.Data.SlopeLeftMm, request.Data.SlopeRightMm,
            request.Data.RawMeasurementsJson,
            request.Data.ObstaclesJson,
            request.Data.PhotoUrlsJson,
            request.Data.AnnotatedPhotoUrlsJson,
            request.Data.Notes);
        _repo.Update(survey);
        return FieldSurveyMapper.ToDto(survey);
    }
}

public class SubmitFieldSurveyCommandHandler : IRequestHandler<SubmitFieldSurveyCommand, FieldSurveyDto>
{
    private readonly IFieldSurveyRepository _repo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public SubmitFieldSurveyCommandHandler(
        IFieldSurveyRepository repo,
        IGlassProjectRepository projectRepo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
    }

    public async Task<FieldSurveyDto> Handle(SubmitFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (survey.Status != FieldSurveyStatus.InProgress)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        survey.Submit();
        _repo.Update(survey);

        var project = await _projectRepo.GetByIdAsync(survey.ProjectId, cancellationToken);
        if (project is not null && project.Status == GlassProjectStatus.Draft)
        {
            project.TransitionTo(GlassProjectStatus.Surveyed, _currentUser.UserId ?? Guid.Empty);
            _projectRepo.Update(project);
        }
        return FieldSurveyMapper.ToDto(survey);
    }
}

public class ApproveFieldSurveyCommandHandler : IRequestHandler<ApproveFieldSurveyCommand, FieldSurveyApplyResultDto?>
{
    private readonly IFieldSurveyRepository _repo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IFieldSurveyApplier _applier;
    private readonly IProjectRecomputeService _recompute;
    private readonly IBomStaleSignal _bomStaleSignal;

    public ApproveFieldSurveyCommandHandler(
        IFieldSurveyRepository repo,
        IGlassProjectRepository projectRepo,
        IFieldSurveyApplier applier,
        IProjectRecomputeService recompute,
        IBomStaleSignal bomStaleSignal)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _applier = applier;
        _recompute = recompute;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<FieldSurveyApplyResultDto?> Handle(ApproveFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (survey.Status != FieldSurveyStatus.Submitted)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        survey.Approve();
        _repo.Update(survey);

        if (!request.Data.ApplyToProject) return null;

        var project = await _projectRepo.GetByIdWithRunsAsync(survey.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        if (project.TenantId != survey.TenantId)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        if (project.Status != GlassProjectStatus.Draft && project.Status != GlassProjectStatus.Surveyed)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        var result = await _applier.ApplyAsync(project, survey, cancellationToken);
        await _recompute.RecalculateAsync(project, cancellationToken);
        survey.MarkApplied();
        _repo.Update(survey);
        _projectRepo.Update(project);
        await _bomStaleSignal.SignalStaleAsync(project.Id, BomStaleReason.SurveyApplied, cancellationToken);

        return new FieldSurveyApplyResultDto(
            result.ProjectId, result.SurveyId, result.RunsUpdated,
            result.MaxSlopeAdjustmentMm, result.ToleranceTopMm, result.ToleranceSideMm);
    }
}

public class RejectFieldSurveyCommandHandler : IRequestHandler<RejectFieldSurveyCommand, FieldSurveyDto>
{
    private readonly IFieldSurveyRepository _repo;
    public RejectFieldSurveyCommandHandler(IFieldSurveyRepository repo) => _repo = repo;

    public async Task<FieldSurveyDto> Handle(RejectFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (survey.Status != FieldSurveyStatus.Submitted)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        survey.Reject(request.Data.Reason);
        _repo.Update(survey);
        return FieldSurveyMapper.ToDto(survey);
    }
}

public class ApplyFieldSurveyCommandHandler : IRequestHandler<ApplyFieldSurveyCommand, FieldSurveyApplyResultDto>
{
    private readonly IFieldSurveyRepository _repo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IFieldSurveyApplier _applier;
    private readonly IProjectRecomputeService _recompute;
    private readonly IBomStaleSignal _bomStaleSignal;

    public ApplyFieldSurveyCommandHandler(
        IFieldSurveyRepository repo,
        IGlassProjectRepository projectRepo,
        IFieldSurveyApplier applier,
        IProjectRecomputeService recompute,
        IBomStaleSignal bomStaleSignal)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _applier = applier;
        _recompute = recompute;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<FieldSurveyApplyResultDto> Handle(ApplyFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (survey.AppliedAtUtc.HasValue)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        if (survey.Status != FieldSurveyStatus.Approved && survey.Status != FieldSurveyStatus.Submitted)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        var project = await _projectRepo.GetByIdWithRunsAsync(survey.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        if (project.TenantId != survey.TenantId)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        var result = await _applier.ApplyAsync(project, survey, cancellationToken);
        await _recompute.RecalculateAsync(project, cancellationToken);
        survey.MarkApplied();
        _repo.Update(survey);
        _projectRepo.Update(project);
        await _bomStaleSignal.SignalStaleAsync(project.Id, BomStaleReason.SurveyApplied, cancellationToken);

        return new FieldSurveyApplyResultDto(
            result.ProjectId, result.SurveyId, result.RunsUpdated,
            result.MaxSlopeAdjustmentMm, result.ToleranceTopMm, result.ToleranceSideMm);
    }
}

public class DeleteFieldSurveyCommandHandler : IRequestHandler<DeleteFieldSurveyCommand, Unit>
{
    private readonly IFieldSurveyRepository _repo;
    public DeleteFieldSurveyCommandHandler(IFieldSurveyRepository repo) => _repo = repo;

    public async Task<Unit> Handle(DeleteFieldSurveyCommand request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("FieldSurvey");
        if (survey.Status != FieldSurveyStatus.InProgress)
        {
            throw new GlassFieldSurveyNotApplicableException();
        }
        _repo.Remove(survey);
        return Unit.Value;
    }
}

public class GetFieldSurveysByProjectQueryHandler : IRequestHandler<GetFieldSurveysByProjectQuery, IReadOnlyList<FieldSurveyDto>>
{
    private readonly IFieldSurveyRepository _repo;
    public GetFieldSurveysByProjectQueryHandler(IFieldSurveyRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<FieldSurveyDto>> Handle(GetFieldSurveysByProjectQuery request, CancellationToken cancellationToken)
    {
        var surveys = await _repo.ListByProjectAsync(request.ProjectId, cancellationToken);
        return surveys.Select(FieldSurveyMapper.ToDto).ToList();
    }
}

public class GetFieldSurveyByIdQueryHandler : IRequestHandler<GetFieldSurveyByIdQuery, FieldSurveyDto?>
{
    private readonly IFieldSurveyRepository _repo;
    public GetFieldSurveyByIdQueryHandler(IFieldSurveyRepository repo) => _repo = repo;

    public async Task<FieldSurveyDto?> Handle(GetFieldSurveyByIdQuery request, CancellationToken cancellationToken)
    {
        var survey = await _repo.GetByIdAsync(request.Id, cancellationToken);
        return survey is null ? null : FieldSurveyMapper.ToDto(survey);
    }
}
