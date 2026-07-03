using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

// WHY: on an arc run panels divide the DEVELOPED length (radius·sweep, the physical glass span) —
// LengthMm is the CHORD (the fixed span between the ends), shorter by up to ×1.57 at 180°.
public static class GlassRunPanelMath
{
    public static int PanelSpanMm(int lengthMm, int? arcRadiusMm, decimal? arcSweepDeg)
    {
        if (arcRadiusMm is > 0 && arcSweepDeg is not null && Math.Abs(arcSweepDeg.Value) >= 0.1m)
        {
            var sweepRad = Math.Min((double)Math.Abs(arcSweepDeg.Value) * Math.PI / 180.0, Math.PI * 2);
            return Math.Max(1, (int)Math.Round(arcRadiusMm.Value * sweepRad));
        }
        return lengthMm;
    }
}

public class CreateGlassProjectCommandHandler : IRequestHandler<CreateGlassProjectCommand, GlassProjectDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IDocumentSequenceRepository _sequenceRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateGlassProjectCommandHandler(
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IDocumentSequenceRepository sequenceRepo,
        ICurrentUserAccessor currentUser)
    {
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _sequenceRepo = sequenceRepo;
        _currentUser = currentUser;
    }

    public async Task<GlassProjectDto> Handle(CreateGlassProjectCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepo.GetByIdAsync(request.Data.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        await _sequenceRepo.EnsureExistsAsync(
            DocumentSequenceType.GlassProjectCode,
            prefix: "GE",
            padLength: 4,
            year: DateTime.UtcNow.Year,
            cancellationToken);

        var code = await _sequenceRepo.ConsumeAsync(DocumentSequenceType.GlassProjectCode, DateTime.UtcNow, cancellationToken);
        var createdByUserId = ResolveCurrentUserId();

        var project = new GlassProject(code, customer.Id, request.Data.ProjectName, createdByUserId, request.Data.Currency);
        project.UpdateHeader(
            projectName: request.Data.ProjectName,
            siteAddressLine1: request.Data.SiteAddressLine1,
            siteAddressLine2: request.Data.SiteAddressLine2,
            siteCity: request.Data.SiteCity,
            siteDistrict: request.Data.SiteDistrict,
            sitePostalCode: request.Data.SitePostalCode,
            siteCountryCode: request.Data.SiteCountryCode,
            floorNumber: request.Data.FloorNumber,
            buildingHeightM: request.Data.BuildingHeightM,
            windZoneId: null,
            climateZoneId: null,
            fireSafetyClass: null,
            scaffoldingRequired: false,
            craneRequired: false,
            validUntilDate: request.Data.ValidUntilDate,
            notes: request.Data.Notes);

        await _projectRepo.AddAsync(project, cancellationToken);
        return ProjectMappers.ToDto(project, customer.Name);
    }

    private Guid ResolveCurrentUserId() => _currentUser.UserId ?? Guid.Empty;
}

public class UpdateGlassProjectHeaderCommandHandler : IRequestHandler<UpdateGlassProjectHeaderCommand, GlassProjectDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;

    public UpdateGlassProjectHeaderCommandHandler(IGlassProjectRepository projectRepo, ICustomerRepository customerRepo)
    {
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
    }

    public async Task<GlassProjectDto> Handle(UpdateGlassProjectHeaderCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.Id, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        project.UpdateHeader(
            request.Data.ProjectName,
            request.Data.SiteAddressLine1,
            request.Data.SiteAddressLine2,
            request.Data.SiteCity,
            request.Data.SiteDistrict,
            request.Data.SitePostalCode,
            request.Data.SiteCountryCode,
            request.Data.FloorNumber,
            request.Data.BuildingHeightM,
            request.Data.WindZoneId,
            request.Data.ClimateZoneId,
            request.Data.FireSafetyClass,
            request.Data.ScaffoldingRequired,
            request.Data.CraneRequired,
            request.Data.ValidUntilDate,
            request.Data.Notes);
        _projectRepo.Update(project);
        var customer = await _customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);
        return ProjectMappers.ToDto(project, customer?.Name);
    }
}

public class AssignGlassProjectTeamCommandHandler : IRequestHandler<AssignGlassProjectTeamCommand, GlassProjectDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    public AssignGlassProjectTeamCommandHandler(IGlassProjectRepository projectRepo, ICustomerRepository customerRepo)
    {
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
    }

    public async Task<GlassProjectDto> Handle(AssignGlassProjectTeamCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.Id, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        project.AssignTeam(request.Data.DesignerUserId, request.Data.SalespersonUserId);
        _projectRepo.Update(project);
        var customer = await _customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);
        return ProjectMappers.ToDto(project, customer?.Name);
    }
}

public class TransitionGlassProjectStatusCommandHandler : IRequestHandler<TransitionGlassProjectStatusCommand, GlassProjectDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ICurrentUserAccessor _currentUser;
    public TransitionGlassProjectStatusCommandHandler(
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        ICurrentUserAccessor currentUser)
    {
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _currentUser = currentUser;
    }

    public async Task<GlassProjectDto> Handle(TransitionGlassProjectStatusCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.Id, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        project.TransitionTo(request.Data.TargetStatus, _currentUser.UserId ?? Guid.Empty);
        _projectRepo.Update(project);
        var customer = await _customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);
        return ProjectMappers.ToDto(project, customer?.Name);
    }
}

public class ConfigureEnclosureCommandHandler : IRequestHandler<ConfigureEnclosureCommand, GlassProjectDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ITemplateRegistry _templateRegistry;

    public ConfigureEnclosureCommandHandler(
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        ITemplateRegistry templateRegistry)
    {
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _templateRegistry = templateRegistry;
    }

    public async Task<GlassProjectDto> Handle(ConfigureEnclosureCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();

        var preset = _templateRegistry.Resolve(request.Data.Subtype);
        var defaults = preset.BuildDefaults();

        var geometryMode = request.Data.GeometryMode ?? preset.DefaultGeometryMode;
        var mountingTopology = request.Data.MountingTopology ?? preset.DefaultMountingTopology;
        var roofPitchDeg = request.Data.RoofPitchDeg ?? defaults.DefaultRoofPitchDeg;

        var input = new EnclosureConfigurationInput(
            request.Data.Category,
            request.Data.Subtype,
            geometryMode,
            mountingTopology,
            roofPitchDeg,
            request.Data.RidgeHeightMm,
            request.Data.EaveHeightMm);

        var validation = preset.Validate(input);
        if (!validation.IsValid)
        {
            var issueKeys = validation.Issues
                .Where(i => i.Severity == EnclosureValidationSeverity.Error)
                .Select(i => i.MessageKey)
                .ToList();
            throw new EnclosureConfigurationInvalidException(issueKeys);
        }

        project.ConfigureEnclosure(
            request.Data.Category,
            request.Data.Subtype,
            geometryMode,
            mountingTopology,
            roofPitchDeg,
            request.Data.RidgeHeightMm,
            request.Data.EaveHeightMm,
            request.Data.CurtainWallCassetteSpecJson,
            request.Data.PolygonVerticesJson,
            request.Data.MetadataJson);

        _projectRepo.Update(project);
        var customer = await _customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);
        return ProjectMappers.ToDto(project, customer?.Name);
    }
}

public class DeleteGlassProjectCommandHandler : IRequestHandler<DeleteGlassProjectCommand, Unit>
{
    private readonly IGlassProjectRepository _projectRepo;
    public DeleteGlassProjectCommandHandler(IGlassProjectRepository projectRepo) => _projectRepo = projectRepo;

    public async Task<Unit> Handle(DeleteGlassProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        if (project.Status != GlassProjectStatus.Draft && project.Status != GlassProjectStatus.Cancelled)
        {
            throw new GlassProjectInvalidStatusTransitionException(project.Status.ToString(), "Deleted");
        }
        _projectRepo.Remove(project);
        return Unit.Value;
    }
}

public class AddRunCommandHandler : IRequestHandler<AddRunCommand, GlassProjectRunDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectRunRepository _runRepo;
    private readonly IProfileSystemRepository _profileSystemRepo;
    private readonly IGlassTypeRepository _glassTypeRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public AddRunCommandHandler(IGlassProjectRepository projectRepo, IGlassProjectRunRepository runRepo, IProfileSystemRepository profileSystemRepo, IGlassTypeRepository glassTypeRepo, IBomStaleSignal bomStaleSignal)
    {
        _projectRepo = projectRepo;
        _runRepo = runRepo;
        _profileSystemRepo = profileSystemRepo;
        _glassTypeRepo = glassTypeRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<GlassProjectRunDto> Handle(AddRunCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        var orderIndex = project.Runs.Count;

        var profileSystemId = request.Data.ProfileSystemId ?? Guid.Empty;
        if (profileSystemId == Guid.Empty)
        {
            var defaultSystem = (await _profileSystemRepo.ListAsync(isActive: true, cancellationToken: cancellationToken)).FirstOrDefault()
                ?? throw new GlassTenantOnboardingIncompleteException();
            profileSystemId = defaultSystem.Id;
        }

        var label = string.IsNullOrWhiteSpace(request.Data.Label) ? $"Run {orderIndex + 1}" : request.Data.Label;

        var run = new GlassProjectRun(
            project.Id, orderIndex, label,
            request.Data.LengthMm, request.Data.HeightMm,
            profileSystemId,
            request.Data.OriginX, request.Data.OriginY, request.Data.RotationDeg,
            request.Data.ColorId, request.Data.HasTopDrip, request.Data.HasBottomThreshold, request.Data.Notes);
        run.UpdateGeometry3D(request.Data.GeomZ, request.Data.GeomTiltDeg, request.Data.GeomArcRadiusMm, request.Data.GeomArcSweepDeg, request.Data.ArcGlassBent ?? false);

        var panelCount = request.Data.PanelCount ?? 0;
        if (panelCount > 0 && request.Data.LengthMm > 0)
        {
            var defaultGlassType = (await _glassTypeRepo.ListAsync(isActive: true, cancellationToken: cancellationToken)).FirstOrDefault();
            if (defaultGlassType is not null)
            {
                var clampedCount = Math.Min(panelCount, 50);
                var spanMm = GlassRunPanelMath.PanelSpanMm(request.Data.LengthMm, request.Data.GeomArcRadiusMm, request.Data.GeomArcSweepDeg);
                var baseWidth = Math.Max(1, spanMm / clampedCount);
                for (var i = 0; i < clampedCount; i++)
                {
                    var widthMm = i == clampedCount - 1 ? Math.Max(1, spanMm - baseWidth * (clampedCount - 1)) : baseWidth;
                    run.AddPanel(new GlassProjectPanel(
                        run.Id, i,
                        widthMm, GlassOpeningType.Fixed, defaultGlassType.Id,
                        false, false, false, null));
                }
            }
        }

        await _runRepo.AddAsync(run, cancellationToken);
        await _bomStaleSignal.SignalStaleAsync(project.Id, BomStaleReason.RunChanged, cancellationToken);
        return ProjectMappers.ToDto(run);
    }
}

public class UpdateRunCommandHandler : IRequestHandler<UpdateRunCommand, GlassProjectRunDto>
{
    private readonly IGlassProjectRunRepository _runRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public UpdateRunCommandHandler(IGlassProjectRunRepository runRepo, IBomStaleSignal bomStaleSignal)
    {
        _runRepo = runRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<GlassProjectRunDto> Handle(UpdateRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _runRepo.GetByIdWithPanelsAsync(request.RunId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProjectRun");
        if (run.ProjectId != request.ProjectId) throw new CrossTenantAccessException();
        run.UpdateGeometry(request.Data.LengthMm, request.Data.HeightMm, request.Data.OriginX, request.Data.OriginY, request.Data.RotationDeg);
        run.UpdateGeometry3D(request.Data.GeomZ, request.Data.GeomTiltDeg, request.Data.GeomArcRadiusMm, request.Data.GeomArcSweepDeg, request.Data.ArcGlassBent ?? false);
        run.UpdateConfiguration(request.Data.Label, request.Data.ProfileSystemId, request.Data.ColorId, request.Data.HasTopDrip, request.Data.HasBottomThreshold, request.Data.Notes);
        _runRepo.Update(run);
        await _bomStaleSignal.SignalStaleAsync(run.ProjectId, BomStaleReason.RunChanged, cancellationToken);
        return ProjectMappers.ToDto(run);
    }
}

public class RemoveRunCommandHandler : IRequestHandler<RemoveRunCommand, Unit>
{
    private readonly IGlassProjectRunRepository _runRepo;
    private readonly IRunConnectionRepository _connectionRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public RemoveRunCommandHandler(
        IGlassProjectRunRepository runRepo,
        IRunConnectionRepository connectionRepo,
        IBomStaleSignal bomStaleSignal)
    {
        _runRepo = runRepo;
        _connectionRepo = connectionRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<Unit> Handle(RemoveRunCommand request, CancellationToken cancellationToken)
    {
        // WHY: deleting an already-deleted run is success — the undo/redo reconciler retries
        // deletes against a base that may have advanced, and a 404 here poisons the whole sync.
        var run = await _runRepo.GetByIdAsync(request.RunId, cancellationToken);
        if (run is null) return Unit.Value;
        if (run.ProjectId != request.ProjectId) throw new CrossTenantAccessException();
        var connections = await _connectionRepo.ListByProjectAsync(run.ProjectId, cancellationToken);
        foreach (var connection in connections)
        {
            if (connection.RunAId == run.Id || connection.RunBId == run.Id)
            {
                _connectionRepo.Remove(connection);
            }
        }
        _runRepo.Remove(run);
        await _bomStaleSignal.SignalStaleAsync(run.ProjectId, BomStaleReason.RunChanged, cancellationToken);
        return Unit.Value;
    }
}

public class BulkRebalancePanelsCommandHandler : IRequestHandler<BulkRebalancePanelsCommand, GlassProjectRunDto>
{
    private readonly IGlassProjectRunRepository _runRepo;
    private readonly IGlassProjectPanelRepository _panelRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public BulkRebalancePanelsCommandHandler(IGlassProjectRunRepository runRepo, IGlassProjectPanelRepository panelRepo, IBomStaleSignal bomStaleSignal)
    {
        _runRepo = runRepo;
        _panelRepo = panelRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<GlassProjectRunDto> Handle(BulkRebalancePanelsCommand request, CancellationToken cancellationToken)
    {
        var run = await _runRepo.GetByIdWithPanelsAsync(request.RunId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProjectRun");
        if (run.ProjectId != request.ProjectId) throw new CrossTenantAccessException();
        var count = Math.Max(1, request.Data.PanelCount);
        var spanMm = GlassRunPanelMath.PanelSpanMm(run.LengthMm, run.GeomArcRadiusMm, run.GeomArcSweepDeg);
        var baseWidth = Math.Max(1, spanMm / count);

        foreach (var existing in run.Panels.ToList()) _panelRepo.Remove(existing);

        var newPanels = Enumerable.Range(0, count).Select(i =>
            new GlassProjectPanel(
                run.Id, i,
                i == count - 1 ? Math.Max(1, spanMm - baseWidth * (count - 1)) : baseWidth,
                request.Data.DefaultOpeningType, request.Data.DefaultGlassTypeId)).ToList();
        run.ReplacePanels(newPanels);
        // WHY: insert/delete the panels explicitly through the DbSet. A graph-walk over the tracked
        // run (DetectChanges or _context.Update) marks new panels with a pre-set Guid PK as Modified,
        // not Added → UPDATE on non-existent rows → DbUpdateConcurrencyException.
        foreach (var panel in newPanels) await _panelRepo.AddAsync(panel, cancellationToken);

        await _bomStaleSignal.SignalStaleAsync(run.ProjectId, BomStaleReason.PanelChanged, cancellationToken);
        return ProjectMappers.ToDto(run);
    }
}

public class AddPanelCommandHandler : IRequestHandler<AddPanelCommand, GlassProjectPanelDto>
{
    private readonly IGlassProjectRunRepository _runRepo;
    private readonly IGlassProjectPanelRepository _panelRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public AddPanelCommandHandler(IGlassProjectRunRepository runRepo, IGlassProjectPanelRepository panelRepo, IBomStaleSignal bomStaleSignal)
    {
        _runRepo = runRepo;
        _panelRepo = panelRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<GlassProjectPanelDto> Handle(AddPanelCommand request, CancellationToken cancellationToken)
    {
        var run = await _runRepo.GetByIdWithPanelsAsync(request.RunId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProjectRun");
        if (run.ProjectId != request.ProjectId) throw new CrossTenantAccessException();
        var panelIndex = run.Panels.Count;
        var panel = new GlassProjectPanel(
            run.Id, panelIndex,
            request.Data.WidthMm, request.Data.OpeningType, request.Data.GlassTypeId,
            request.Data.HasHandle, request.Data.HasLock, request.Data.HasBrushSeal, request.Data.Notes);
        panel.UpdateShape(
            request.Data.HeightMm, request.Data.TopShape, request.Data.TopRightHeightMm,
            request.Data.ArchRiseMm,
            request.Data.CornerRadiiMm?.Tl, request.Data.CornerRadiiMm?.Tr,
            request.Data.CornerRadiiMm?.Br, request.Data.CornerRadiiMm?.Bl,
            request.Data.ShapeKind, request.Data.ShapePointsJson);
        await _panelRepo.AddAsync(panel, cancellationToken);
        await _bomStaleSignal.SignalStaleAsync(run.ProjectId, BomStaleReason.PanelChanged, cancellationToken);
        return ProjectMappers.ToDto(panel);
    }
}

public class UpdatePanelCommandHandler : IRequestHandler<UpdatePanelCommand, GlassProjectPanelDto>
{
    private readonly IGlassProjectPanelRepository _panelRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public UpdatePanelCommandHandler(IGlassProjectPanelRepository panelRepo, IBomStaleSignal bomStaleSignal)
    {
        _panelRepo = panelRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<GlassProjectPanelDto> Handle(UpdatePanelCommand request, CancellationToken cancellationToken)
    {
        var panel = await _panelRepo.GetByIdAsync(request.PanelId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProjectPanel");
        if (panel.RunId != request.RunId) throw new CrossTenantAccessException();
        panel.Update(
            request.Data.WidthMm, request.Data.OpeningType, request.Data.GlassTypeId,
            request.Data.HasHandle, request.Data.HasLock, request.Data.HasBrushSeal, request.Data.Notes);
        panel.UpdateShape(
            request.Data.HeightMm, request.Data.TopShape, request.Data.TopRightHeightMm,
            request.Data.ArchRiseMm,
            request.Data.CornerRadiiMm?.Tl, request.Data.CornerRadiiMm?.Tr,
            request.Data.CornerRadiiMm?.Br, request.Data.CornerRadiiMm?.Bl,
            request.Data.ShapeKind, request.Data.ShapePointsJson);
        _panelRepo.Update(panel);
        await _bomStaleSignal.SignalStaleAsync(request.ProjectId, BomStaleReason.PanelChanged, cancellationToken);
        return ProjectMappers.ToDto(panel);
    }
}

public class RemovePanelCommandHandler : IRequestHandler<RemovePanelCommand, Unit>
{
    private readonly IGlassProjectPanelRepository _panelRepo;
    private readonly IBomStaleSignal _bomStaleSignal;
    public RemovePanelCommandHandler(IGlassProjectPanelRepository panelRepo, IBomStaleSignal bomStaleSignal)
    {
        _panelRepo = panelRepo;
        _bomStaleSignal = bomStaleSignal;
    }

    public async Task<Unit> Handle(RemovePanelCommand request, CancellationToken cancellationToken)
    {
        var panel = await _panelRepo.GetByIdAsync(request.PanelId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProjectPanel");
        if (panel.RunId != request.RunId) throw new CrossTenantAccessException();
        _panelRepo.Remove(panel);
        await _bomStaleSignal.SignalStaleAsync(request.ProjectId, BomStaleReason.PanelChanged, cancellationToken);
        return Unit.Value;
    }
}

public class AddRunConnectionCommandHandler : IRequestHandler<AddRunConnectionCommand, RunConnectionDto>
{
    private readonly IRunConnectionRepository _repo;
    public AddRunConnectionCommandHandler(IRunConnectionRepository repo) => _repo = repo;

    public async Task<RunConnectionDto> Handle(AddRunConnectionCommand request, CancellationToken cancellationToken)
    {
        var conn = new RunConnection(
            request.ProjectId, request.Data.RunAId, request.Data.RunBId,
            request.Data.JointAngleDeg, request.Data.MitreCutDeg,
            request.Data.UsesCornerPost, request.Data.CornerProfileId);
        await _repo.AddAsync(conn, cancellationToken);
        return ProjectMappers.ToDto(conn);
    }
}

public class UpdateRunConnectionCommandHandler : IRequestHandler<UpdateRunConnectionCommand, RunConnectionDto>
{
    private readonly IRunConnectionRepository _repo;
    public UpdateRunConnectionCommandHandler(IRunConnectionRepository repo) => _repo = repo;

    public async Task<RunConnectionDto> Handle(UpdateRunConnectionCommand request, CancellationToken cancellationToken)
    {
        var conn = await _repo.GetByIdAsync(request.ConnectionId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("RunConnection");
        if (conn.ProjectId != request.ProjectId) throw new CrossTenantAccessException();
        conn.Update(request.Data.JointAngleDeg, request.Data.MitreCutDeg, request.Data.UsesCornerPost, request.Data.CornerProfileId);
        _repo.Update(conn);
        return ProjectMappers.ToDto(conn);
    }
}

public class RemoveRunConnectionCommandHandler : IRequestHandler<RemoveRunConnectionCommand, Unit>
{
    private readonly IRunConnectionRepository _repo;
    public RemoveRunConnectionCommandHandler(IRunConnectionRepository repo) => _repo = repo;

    public async Task<Unit> Handle(RemoveRunConnectionCommand request, CancellationToken cancellationToken)
    {
        var conn = await _repo.GetByIdAsync(request.ConnectionId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("RunConnection");
        if (conn.ProjectId != request.ProjectId) throw new CrossTenantAccessException();
        _repo.Remove(conn);
        return Unit.Value;
    }
}

public class SaveSceneCommandHandler : IRequestHandler<SaveSceneCommand, SceneVersionDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly IGlassProjectSceneRepository _sceneRepo;
    private readonly ISceneCompressor _compressor;
    private readonly ICurrentUserAccessor _currentUser;

    public SaveSceneCommandHandler(
        IGlassProjectRepository projectRepo,
        IGlassProjectSceneRepository sceneRepo,
        ISceneCompressor compressor,
        ICurrentUserAccessor currentUser)
    {
        _projectRepo = projectRepo;
        _sceneRepo = sceneRepo;
        _compressor = compressor;
        _currentUser = currentUser;
    }

    public async Task<SceneVersionDto> Handle(SaveSceneCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();

        var maxVersion = await _sceneRepo.GetMaxVersionAsync(project.Id, cancellationToken);
        var newVersion = maxVersion + 1;
        var compressed = _compressor.Compress(request.Data.SceneJson);
        var savedBy = _currentUser.UserId ?? Guid.Empty;

        var scene = new GlassProjectScene(
            project.Id, newVersion, compressed, savedBy,
            thumbnailUrl: null, cameraStateJson: request.Data.CameraStateJson, label: request.Data.Label);
        await _sceneRepo.AddAsync(scene, cancellationToken);

        project.AdvanceSceneVersion(newVersion);
        _projectRepo.Update(project);

        return new SceneVersionDto(scene.Id, scene.Version, scene.Label, scene.ThumbnailUrl, scene.SavedByUserId, scene.SavedAtUtc, scene.IsCustomerApproved);
    }
}

public class ValidateProjectCommandHandler : IRequestHandler<ValidateProjectCommand, GlassProjectValidationResultDto>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ISceneValidator _validator;
    public ValidateProjectCommandHandler(IGlassProjectRepository projectRepo, ISceneValidator validator)
    {
        _projectRepo = projectRepo;
        _validator = validator;
    }

    public async Task<GlassProjectValidationResultDto> Handle(ValidateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.ProjectId, cancellationToken)
            ?? throw new GlassProjectNotFoundException();
        return await _validator.ValidateAsync(project, cancellationToken);
    }
}

public class GetGlassProjectsQueryHandler : IRequestHandler<GetGlassProjectsQuery, PagedResult<GlassProjectListItemDto>>
{
    private readonly IGlassProjectRepository _projectRepo;
    public GetGlassProjectsQueryHandler(IGlassProjectRepository projectRepo) => _projectRepo = projectRepo;

    public async Task<PagedResult<GlassProjectListItemDto>> Handle(GetGlassProjectsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _projectRepo.SearchAsync(
            request.Search, request.Status, request.CustomerId,
            request.AssignedDesignerUserId, request.AssignedSalespersonUserId,
            Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100),
            cancellationToken);

        return new PagedResult<GlassProjectListItemDto>
        {
            Items = items.Select(ProjectMappers.ToDto).ToList(),
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}

public class GetGlassProjectByIdQueryHandler : IRequestHandler<GetGlassProjectByIdQuery, GlassProjectDto?>
{
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    public GetGlassProjectByIdQueryHandler(IGlassProjectRepository projectRepo, ICustomerRepository customerRepo)
    {
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
    }

    public async Task<GlassProjectDto?> Handle(GetGlassProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByIdWithRunsAsync(request.Id, cancellationToken);
        if (project is null) return null;
        var customer = await _customerRepo.GetByIdAsync(project.CustomerId, cancellationToken);
        return ProjectMappers.ToDto(project, customer?.Name);
    }
}

public class GetSceneLatestQueryHandler : IRequestHandler<GetSceneLatestQuery, SceneLatestDto?>
{
    private readonly IGlassProjectSceneRepository _sceneRepo;
    private readonly ISceneCompressor _compressor;
    public GetSceneLatestQueryHandler(IGlassProjectSceneRepository sceneRepo, ISceneCompressor compressor)
    {
        _sceneRepo = sceneRepo;
        _compressor = compressor;
    }

    public async Task<SceneLatestDto?> Handle(GetSceneLatestQuery request, CancellationToken cancellationToken)
    {
        var scene = await _sceneRepo.GetLatestAsync(request.ProjectId, cancellationToken);
        if (scene is null) return null;
        var json = _compressor.Decompress(scene.SceneJsonCompressed);
        return new SceneLatestDto(scene.Version, json, scene.CameraStateJson, scene.ThumbnailUrl, scene.SavedAtUtc);
    }
}

public class GetSceneVersionsQueryHandler : IRequestHandler<GetSceneVersionsQuery, IReadOnlyList<SceneVersionDto>>
{
    private readonly IGlassProjectSceneRepository _sceneRepo;
    public GetSceneVersionsQueryHandler(IGlassProjectSceneRepository sceneRepo) => _sceneRepo = sceneRepo;

    public async Task<IReadOnlyList<SceneVersionDto>> Handle(GetSceneVersionsQuery request, CancellationToken cancellationToken)
    {
        var versions = await _sceneRepo.ListVersionsAsync(request.ProjectId, request.Limit, cancellationToken);
        return versions
            .Select(s => new SceneVersionDto(s.Id, s.Version, s.Label, s.ThumbnailUrl, s.SavedByUserId, s.SavedAtUtc, s.IsCustomerApproved))
            .ToList();
    }
}

public class GetSceneByVersionQueryHandler : IRequestHandler<GetSceneByVersionQuery, SceneLatestDto?>
{
    private readonly IGlassProjectSceneRepository _sceneRepo;
    private readonly ISceneCompressor _compressor;
    public GetSceneByVersionQueryHandler(IGlassProjectSceneRepository sceneRepo, ISceneCompressor compressor)
    {
        _sceneRepo = sceneRepo;
        _compressor = compressor;
    }

    public async Task<SceneLatestDto?> Handle(GetSceneByVersionQuery request, CancellationToken cancellationToken)
    {
        var scene = await _sceneRepo.GetByVersionAsync(request.ProjectId, request.Version, cancellationToken);
        if (scene is null) return null;
        var json = _compressor.Decompress(scene.SceneJsonCompressed);
        return new SceneLatestDto(scene.Version, json, scene.CameraStateJson, scene.ThumbnailUrl, scene.SavedAtUtc);
    }
}
