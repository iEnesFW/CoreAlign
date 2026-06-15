using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.Templates;

public class ProjectTemplateServiceTests
{
    private readonly IProjectTemplateRepository _templateRepo = Substitute.For<IProjectTemplateRepository>();
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepo = Substitute.For<IDocumentSequenceRepository>();
    private readonly IProfileSystemRepository _profileSystemRepo = Substitute.For<IProfileSystemRepository>();
    private readonly IGlassTypeRepository _glassTypeRepo = Substitute.For<IGlassTypeRepository>();
    private readonly ITemplateRegistry _templateRegistry = Substitute.For<ITemplateRegistry>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();

    private ProjectTemplateService BuildSut() => new(
        _templateRepo, _projectRepo, _customerRepo, _sequenceRepo,
        _profileSystemRepo, _glassTypeRepo, _templateRegistry, _currentUser);

    [Fact]
    public async Task CreateProjectFromTemplate_throws_when_template_not_found()
    {
        var templateId = Guid.NewGuid();
        _templateRepo.GetByIdWithPresetsAsync(templateId, Arg.Any<CancellationToken>())
            .Returns((ProjectTemplate?)null);

        var sut = BuildSut();

        var act = async () => await sut.CreateProjectFromTemplateAsync(
            templateId, Guid.NewGuid(), "Project X", "TRY", default);

        await act.Should().ThrowAsync<EnclosurePresetNotFoundException>();
        await _projectRepo.DidNotReceive().AddAsync(Arg.Any<GlassProject>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProjectFromTemplate_creates_project_with_run_count_matching_presets()
    {
        var template = BuildBalconyTemplate(runPresetCount: 3);
        SetupHappyPath(template);

        var sut = BuildSut();

        await sut.CreateProjectFromTemplateAsync(template.Id, Guid.NewGuid(), "Test Project", "TRY", default);

        await _projectRepo.Received(1).AddAsync(
            Arg.Is<GlassProject>(p => p.Runs.Count == template.RunPresets.Count),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProjectFromTemplate_copies_enclosure_configuration_from_template()
    {
        var template = BuildBalconyTemplate(runPresetCount: 1);
        SetupHappyPath(template);

        var sut = BuildSut();

        GlassProject? captured = null;
        await _projectRepo.AddAsync(
            Arg.Do<GlassProject>(p => captured = p),
            Arg.Any<CancellationToken>());

        await sut.CreateProjectFromTemplateAsync(template.Id, Guid.NewGuid(), "Cfg Project", "TRY", default);

        captured.Should().NotBeNull();
        captured!.EnclosureCategory.Should().Be(template.Category);
        captured.EnclosureSubtype.Should().Be(template.Subtype);
        captured.GeometryMode.Should().Be(template.GeometryMode);
        captured.MountingTopology.Should().Be(template.MountingTopology);
    }

    [Fact]
    public async Task ListAsync_with_functional_category_returns_only_functional_templates()
    {
        var functional = BuildTemplate("FN-1", EnclosureCategory.Functional, EnclosureSubtype.ShowerCabin);
        _templateRepo.ListAsync(EnclosureCategory.Functional, true, Arg.Any<CancellationToken>())
            .Returns(new List<ProjectTemplate> { functional });

        var sut = BuildSut();

        var result = await sut.ListAsync(EnclosureCategory.Functional, default);

        result.Should().ContainSingle();
        result[0].Category.Should().Be(EnclosureCategory.Functional);
        result[0].Subtype.Should().Be(EnclosureSubtype.ShowerCabin);
        await _templateRepo.Received(1).ListAsync(EnclosureCategory.Functional, true, Arg.Any<CancellationToken>());
    }

    private static ProjectTemplate BuildTemplate(string code, EnclosureCategory category, EnclosureSubtype subtype) => new(
        code: code,
        displayNameKey: $"Templates.{code}",
        isSystemTemplate: true,
        category: category,
        subtype: subtype,
        geometryMode: GeometryMode.Planar,
        mountingTopology: MountingTopology.ProfileFramed,
        defaultConnectorKind: ConnectorKind.Profile);

    private static ProjectTemplate BuildBalconyTemplate(int runPresetCount)
    {
        var template = BuildTemplate("L-BALKON", EnclosureCategory.Vertical, EnclosureSubtype.Balcony);
        for (var i = 0; i < runPresetCount; i++)
        {
            template.AddRunPreset(new ProjectTemplateRunPreset(
                templateId: template.Id,
                orderIndex: i,
                labelKey: $"Run.{i + 1}",
                lengthMm: 3000,
                heightMm: 2400,
                defaultPanelCount: 3,
                defaultPanelWidthMm: 1000,
                defaultOpeningType: GlassOpeningType.SlidingLeft,
                connectsToPreviousAsCorner: i > 0,
                cornerJointAngleDeg: i > 0 ? 90m : null,
                cornerUsesPost: i > 0));
        }
        return template;
    }

    private void SetupHappyPath(ProjectTemplate template)
    {
        _templateRepo.GetByIdWithPresetsAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var customer = new Customer("Acme") { TenantId = Guid.NewGuid() };
        _customerRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(customer);

        var profileSystem = new ProfileSystem(
            code: "PS-DEFAULT",
            name: "Default System",
            brandId: Guid.NewGuid(),
            systemType: GlassSystemType.Sliding,
            maxPanelWidthMm: 1200,
            maxPanelHeightMm: 2700,
            maxPanelWeightKg: 80m,
            supportedGlassThicknessesJson: "[8,10]",
            supportedOpeningsJson: "[0,2,3]");
        _profileSystemRepo.ListAsync(true, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProfileSystem> { profileSystem });

        var glassType = new GlassType(
            code: "GL-DEFAULT",
            name: "Default Glass",
            thicknessMm: 8,
            structure: GlassStructure.Tempered,
            pricePerM2: 100m,
            weightKgPerM2: 20m,
            allowablePressurePa: 2000m,
            maxPanelAreaM2: 6m,
            uValue: 5.7m,
            soundDb: 30m);
        _glassTypeRepo.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<GlassType> { glassType });

        _sequenceRepo.EnsureExistsAsync(
            Arg.Any<DocumentSequenceType>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _sequenceRepo.ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("GE-0001");

        _currentUser.UserId.Returns(Guid.NewGuid());
        _templateRegistry.Find(Arg.Any<EnclosureSubtype>()).Returns((IEnclosurePreset?)null);
    }
}
