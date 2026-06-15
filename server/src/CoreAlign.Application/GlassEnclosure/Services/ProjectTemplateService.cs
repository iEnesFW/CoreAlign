using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Services;

public class ProjectTemplateService : IProjectTemplateService
{
    private readonly IProjectTemplateRepository _templateRepo;
    private readonly IGlassProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IDocumentSequenceRepository _sequenceRepo;
    private readonly IProfileSystemRepository _profileSystemRepo;
    private readonly IGlassTypeRepository _glassTypeRepo;
    private readonly ITemplateRegistry _templateRegistry;
    private readonly ICurrentUserAccessor _currentUser;

    public ProjectTemplateService(
        IProjectTemplateRepository templateRepo,
        IGlassProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IDocumentSequenceRepository sequenceRepo,
        IProfileSystemRepository profileSystemRepo,
        IGlassTypeRepository glassTypeRepo,
        ITemplateRegistry templateRegistry,
        ICurrentUserAccessor currentUser)
    {
        _templateRepo = templateRepo;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _sequenceRepo = sequenceRepo;
        _profileSystemRepo = profileSystemRepo;
        _glassTypeRepo = glassTypeRepo;
        _templateRegistry = templateRegistry;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProjectTemplateSummaryDto>> ListAsync(
        EnclosureCategory? category,
        CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepo.ListAsync(category, isActive: true, cancellationToken);
        return templates.Select(ProjectTemplateMappers.ToSummary).ToList();
    }

    public async Task<ProjectTemplateDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetByIdWithPresetsAsync(id, cancellationToken);
        return template is null ? null : ProjectTemplateMappers.ToDetail(template);
    }

    public async Task<GlassProjectDto> CreateProjectFromTemplateAsync(
        Guid templateId,
        Guid customerId,
        string projectName,
        string? currency,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetByIdWithPresetsAsync(templateId, cancellationToken)
            ?? throw new EnclosurePresetNotFoundException(EnclosureSubtype.Balcony);

        var customer = await _customerRepo.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var profileSystems = await _profileSystemRepo.ListAsync(isActive: true, brandId: null, systemType: null, cancellationToken);
        var defaultProfileSystem = profileSystems.FirstOrDefault()
            ?? throw new EnclosureConfigurationInvalidException(new[] { "GlassEnclosure.Template.NoProfileSystemAvailable" });

        var glassTypes = await _glassTypeRepo.ListAsync(isActive: true, structure: null, cancellationToken);
        var defaultGlassType = glassTypes.FirstOrDefault()
            ?? throw new EnclosureConfigurationInvalidException(new[] { "GlassEnclosure.Template.NoGlassTypeAvailable" });

        await _sequenceRepo.EnsureExistsAsync(
            DocumentSequenceType.GlassProjectCode,
            prefix: "GE",
            padLength: 4,
            year: DateTime.UtcNow.Year,
            cancellationToken);
        var code = await _sequenceRepo.ConsumeAsync(DocumentSequenceType.GlassProjectCode, DateTime.UtcNow, cancellationToken);

        var createdByUserId = _currentUser.UserId ?? Guid.Empty;
        var resolvedCurrency = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency;

        var project = new GlassProject(code, customer.Id, projectName, createdByUserId, resolvedCurrency);

        var preset = _templateRegistry.Find(template.Subtype);
        project.ConfigureEnclosure(
            category: template.Category,
            subtype: template.Subtype,
            geometryMode: template.GeometryMode,
            mountingTopology: template.MountingTopology,
            roofPitchDeg: template.RoofPitchDeg,
            ridgeHeightMm: template.RidgeHeightMm,
            eaveHeightMm: template.EaveHeightMm,
            curtainWallCassetteSpecJson: null,
            polygonVerticesJson: null,
            metadataJson: template.MetadataJson);

        if (preset is not null)
        {
            var validation = preset.Validate(new EnclosureConfigurationInput(
                template.Category, template.Subtype, template.GeometryMode, template.MountingTopology,
                template.RoofPitchDeg, template.RidgeHeightMm, template.EaveHeightMm));
            if (!validation.IsValid)
            {
                var issueKeys = validation.Issues
                    .Where(i => i.Severity == EnclosureValidationSeverity.Error)
                    .Select(i => i.MessageKey)
                    .ToList();
                throw new EnclosureConfigurationInvalidException(issueKeys);
            }
        }

        GlassProjectRun? previousRun = null;
        var runIndex = 0;
        var orderedPresets = template.RunPresets.OrderBy(p => p.OrderIndex).ToList();
        foreach (var runPreset in orderedPresets)
        {
            var run = new GlassProjectRun(
                projectId: project.Id,
                orderIndex: runIndex,
                label: runPreset.LabelKey,
                lengthMm: runPreset.LengthMm,
                heightMm: runPreset.HeightMm,
                profileSystemId: defaultProfileSystem.Id,
                originX: runPreset.OriginX,
                originY: runPreset.OriginY,
                rotationDeg: runPreset.RotationDeg,
                colorId: null,
                hasTopDrip: runPreset.HasTopDrip,
                hasBottomThreshold: runPreset.HasBottomThreshold,
                notes: null);

            for (var i = 0; i < runPreset.DefaultPanelCount; i++)
            {
                run.AddPanel(new GlassProjectPanel(
                    runId: run.Id,
                    panelIndex: i,
                    widthMm: runPreset.DefaultPanelWidthMm,
                    openingType: runPreset.DefaultOpeningType,
                    glassTypeId: defaultGlassType.Id));
            }

            project.AddRun(run);

            if (runPreset.ConnectsToPreviousAsCorner && previousRun is not null)
            {
                project.AddConnection(new RunConnection(
                    projectId: project.Id,
                    runAId: previousRun.Id,
                    runBId: run.Id,
                    jointAngleDeg: runPreset.CornerJointAngleDeg ?? 90m,
                    mitreCutDeg: 45m,
                    usesCornerPost: runPreset.CornerUsesPost,
                    cornerProfileId: null));
            }

            previousRun = run;
            runIndex++;
        }

        await _projectRepo.AddAsync(project, cancellationToken);
        return ProjectMappers.ToDto(project, customer.Name);
    }
}
