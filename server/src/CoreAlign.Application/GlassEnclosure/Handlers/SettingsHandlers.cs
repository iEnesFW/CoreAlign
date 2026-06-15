using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class GetGlassEnclosureSettingsQueryHandler : IRequestHandler<GetGlassEnclosureSettingsQuery, GlassEnclosureSettingsDto>
{
    private readonly IGlassEnclosureSettingsRepository _repository;
    public GetGlassEnclosureSettingsQueryHandler(IGlassEnclosureSettingsRepository repository) => _repository = repository;

    public async Task<GlassEnclosureSettingsDto> Handle(GetGlassEnclosureSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetOrCreateForCurrentTenantAsync(cancellationToken);
        return GlassEnclosureMappers.ToDto(settings);
    }
}

public class UpdateGlassEnclosureSettingsCoreCommandHandler : IRequestHandler<UpdateGlassEnclosureSettingsCoreCommand, GlassEnclosureSettingsDto>
{
    private readonly IGlassEnclosureSettingsRepository _repository;
    public UpdateGlassEnclosureSettingsCoreCommandHandler(IGlassEnclosureSettingsRepository repository) => _repository = repository;

    public async Task<GlassEnclosureSettingsDto> Handle(UpdateGlassEnclosureSettingsCoreCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetOrCreateForCurrentTenantAsync(cancellationToken);
        settings.UpdateCore(
            request.Data.DefaultStockBarLengthMm,
            request.Data.DefaultJumboGlassWidthMm,
            request.Data.DefaultJumboGlassHeightMm,
            request.Data.SawKerfMm,
            request.Data.GlassKerfMm,
            request.Data.GuillotineRequired,
            request.Data.DefaultWastePercent,
            request.Data.LaborCostPerM2,
            request.Data.DefaultMarginPercent,
            request.Data.BendRailFeePerM,
            request.Data.BentGlassCostFactor);
        _repository.Update(settings);
        return GlassEnclosureMappers.ToDto(settings);
    }
}

public class UpdateGlassEnclosureSettingsFieldCommandHandler : IRequestHandler<UpdateGlassEnclosureSettingsFieldCommand, GlassEnclosureSettingsDto>
{
    private readonly IGlassEnclosureSettingsRepository _repository;
    public UpdateGlassEnclosureSettingsFieldCommandHandler(IGlassEnclosureSettingsRepository repository) => _repository = repository;

    public async Task<GlassEnclosureSettingsDto> Handle(UpdateGlassEnclosureSettingsFieldCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetOrCreateForCurrentTenantAsync(cancellationToken);
        settings.UpdateField(request.Data.FieldToleranceTopMm, request.Data.FieldToleranceSideMm);
        _repository.Update(settings);
        return GlassEnclosureMappers.ToDto(settings);
    }
}

public class UpdateGlassEnclosureSettingsInstallationCommandHandler : IRequestHandler<UpdateGlassEnclosureSettingsInstallationCommand, GlassEnclosureSettingsDto>
{
    private readonly IGlassEnclosureSettingsRepository _repository;
    public UpdateGlassEnclosureSettingsInstallationCommandHandler(IGlassEnclosureSettingsRepository repository) => _repository = repository;

    public async Task<GlassEnclosureSettingsDto> Handle(UpdateGlassEnclosureSettingsInstallationCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetOrCreateForCurrentTenantAsync(cancellationToken);
        settings.UpdateInstallation(
            request.Data.TransportRatePerKm,
            request.Data.TransportRatePerKg,
            request.Data.ScaffoldingRequiredFromFloor,
            request.Data.ScaffoldingRatePerM2,
            request.Data.CraneRequiredFromFloor,
            request.Data.CraneRatePerMeter,
            request.Data.WorkshopDailyCapacityM2);
        _repository.Update(settings);
        return GlassEnclosureMappers.ToDto(settings);
    }
}

public class UpdateGlassEnclosureSettingsLocaleCommandHandler : IRequestHandler<UpdateGlassEnclosureSettingsLocaleCommand, GlassEnclosureSettingsDto>
{
    private readonly IGlassEnclosureSettingsRepository _repository;
    public UpdateGlassEnclosureSettingsLocaleCommandHandler(IGlassEnclosureSettingsRepository repository) => _repository = repository;

    public async Task<GlassEnclosureSettingsDto> Handle(UpdateGlassEnclosureSettingsLocaleCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repository.GetOrCreateForCurrentTenantAsync(cancellationToken);
        settings.UpdateLocaleAndCommunication(
            request.Data.DefaultLocale,
            request.Data.DefaultCurrency,
            GlassEnclosureMappers.SerializeStringArray(request.Data.DefaultPaymentTerms),
            request.Data.WhatsappBusinessPhoneId,
            request.Data.NotificationEmailFrom,
            request.Data.QuoteShareTokenTtlDays,
            request.Data.DataRetentionDays);
        _repository.Update(settings);
        return GlassEnclosureMappers.ToDto(settings);
    }
}

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly IProfileSystemRepository _systemRepo;
    private readonly IGlassTypeRepository _glassRepo;
    private readonly IHardwareItemRepository _hardwareRepo;
    private readonly IColorOptionRepository _colorRepo;

    public GetOnboardingStatusQueryHandler(
        IGlassEnclosureSettingsRepository settingsRepo,
        IProfileSystemRepository systemRepo,
        IGlassTypeRepository glassRepo,
        IHardwareItemRepository hardwareRepo,
        IColorOptionRepository colorRepo)
    {
        _settingsRepo = settingsRepo;
        _systemRepo = systemRepo;
        _glassRepo = glassRepo;
        _hardwareRepo = hardwareRepo;
        _colorRepo = colorRepo;
    }

    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepo.GetForCurrentTenantAsync(cancellationToken);
        var systems = await _systemRepo.ListAsync(isActive: null, cancellationToken: cancellationToken);
        var glassTypes = await _glassRepo.ListAsync(isActive: null, cancellationToken: cancellationToken);
        var hardware = await _hardwareRepo.ListAsync(isActive: null, cancellationToken: cancellationToken);
        var colors = await _colorRepo.ListAsync(isActive: null, cancellationToken: cancellationToken);

        var isComplete = settings?.OnboardingComplete ?? false;
        var brandsSelected = systems.Any();
        var workshopConfigured = settings is not null && settings.LaborCostPerM2 > 0;
        var demoSeeded = systems.Any() && glassTypes.Any() && hardware.Any();

        return new OnboardingStatusDto(
            IsComplete: isComplete,
            BrandsSelected: brandsSelected,
            WorkshopConfigured: workshopConfigured,
            DemoSeeded: demoSeeded,
            TotalProfileSystems: systems.Count,
            TotalGlassTypes: glassTypes.Count,
            TotalHardwareItems: hardware.Count,
            TotalColors: colors.Count);
    }
}

public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand, OnboardingStatusDto>
{
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;
    private readonly IMediator _mediator;

    public CompleteOnboardingCommandHandler(IGlassEnclosureSettingsRepository settingsRepo, IMediator mediator)
    {
        _settingsRepo = settingsRepo;
        _mediator = mediator;
    }

    public async Task<OnboardingStatusDto> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var stateJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            request.Data.SelectedBrandCodes,
            request.Data.SeedDemoCatalog,
            CompletedAtUtc = DateTime.UtcNow,
        });
        settings.MarkOnboardingComplete(stateJson);
        _settingsRepo.Update(settings);

        return await _mediator.Send(new GetOnboardingStatusQuery(), cancellationToken);
    }
}
