using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure.Templates;

public class CreateProjectFromTemplateHandlerTests
{
    private readonly IProjectTemplateService _service = Substitute.For<IProjectTemplateService>();

    [Fact]
    public async Task Handle_invokes_service_with_command_payload()
    {
        var dto = new CreateProjectFromTemplateDto(
            TemplateId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            ProjectName: "Pipeline Project",
            Currency: "EUR");
        var sut = new CreateProjectFromTemplateCommandHandler(_service);

        await sut.Handle(new CreateProjectFromTemplateCommand(dto), default);

        await _service.Received(1).CreateProjectFromTemplateAsync(
            dto.TemplateId, dto.CustomerId, dto.ProjectName, dto.Currency, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_project_dto_from_service()
    {
        var expectedId = Guid.NewGuid();
        var expectedDto = BuildProjectDto(expectedId, "Returned Project");
        _service.CreateProjectFromTemplateAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);
        var sut = new CreateProjectFromTemplateCommandHandler(_service);
        var command = new CreateProjectFromTemplateCommand(new CreateProjectFromTemplateDto(
            Guid.NewGuid(), Guid.NewGuid(), "Whatever", null));

        var result = await sut.Handle(command, default);

        result.Should().NotBeNull();
        result.Id.Should().Be(expectedId);
        result.ProjectName.Should().Be("Returned Project");
    }

    private static GlassProjectDto BuildProjectDto(Guid id, string name) => new(
        Id: id,
        Code: "GE-0001",
        CustomerId: Guid.NewGuid(),
        CustomerName: "Acme",
        ProjectName: name,
        SiteAddressLine1: null,
        SiteAddressLine2: null,
        SiteCity: null,
        SiteDistrict: null,
        SitePostalCode: null,
        SiteCountryCode: null,
        Status: GlassProjectStatus.Draft,
        CreatedByUserId: Guid.NewGuid(),
        AssignedDesignerUserId: null,
        AssignedSalespersonUserId: null,
        FloorNumber: null,
        BuildingHeightM: null,
        WindZoneId: null,
        ClimateZoneId: null,
        FireSafetyClass: null,
        ScaffoldingRequired: false,
        CraneRequired: false,
        TotalAreaM2: 0m,
        TotalPanels: 0,
        Subtotal: 0m,
        DiscountTotal: 0m,
        TaxTotal: 0m,
        GrandTotal: 0m,
        Currency: "TRY",
        FxRateToBase: 1m,
        FxRateLockedAtUtc: null,
        WindLoadPaCalculated: null,
        WeightedUValue: null,
        WeightedSoundDb: null,
        ValidUntilDate: null,
        CurrentSceneVersion: 0,
        Notes: null,
        IsBomStale: false,
        BomStaleReason: null,
        StaleSinceUtc: null,
        EnclosureCategory: EnclosureCategory.Vertical,
        EnclosureSubtype: EnclosureSubtype.Balcony,
        GeometryMode: GeometryMode.Planar,
        MountingTopology: MountingTopology.ProfileFramed,
        PolygonVerticesJson: null,
        RoofPitchDeg: null,
        RidgeHeightMm: null,
        EaveHeightMm: null,
        CreatedAtUtc: DateTime.UtcNow,
        UpdatedAtUtc: DateTime.UtcNow,
        Runs: Array.Empty<GlassProjectRunDto>(),
        Connections: Array.Empty<RunConnectionDto>());
}
