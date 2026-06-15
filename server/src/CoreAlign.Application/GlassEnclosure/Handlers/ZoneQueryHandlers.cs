using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class GetWindZonesQueryHandler : IRequestHandler<GetWindZonesQuery, IReadOnlyList<WindZoneDto>>
{
    private readonly IWindZoneRepository _repository;

    public GetWindZonesQueryHandler(IWindZoneRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<WindZoneDto>> Handle(GetWindZonesQuery request, CancellationToken cancellationToken)
    {
        var zones = await _repository.ListAsync(request.IsActive, cancellationToken);
        return zones.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetWindZoneByCodeQueryHandler : IRequestHandler<GetWindZoneByCodeQuery, WindZoneDto?>
{
    private readonly IWindZoneRepository _repository;

    public GetWindZoneByCodeQueryHandler(IWindZoneRepository repository) => _repository = repository;

    public async Task<WindZoneDto?> Handle(GetWindZoneByCodeQuery request, CancellationToken cancellationToken)
    {
        var zone = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        return zone is null ? null : GlassEnclosureMappers.ToDto(zone);
    }
}

public class GetClimateZonesQueryHandler : IRequestHandler<GetClimateZonesQuery, IReadOnlyList<ClimateZoneDto>>
{
    private readonly IClimateZoneRepository _repository;

    public GetClimateZonesQueryHandler(IClimateZoneRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ClimateZoneDto>> Handle(GetClimateZonesQuery request, CancellationToken cancellationToken)
    {
        var zones = await _repository.ListAsync(request.IsActive, cancellationToken);
        return zones.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetClimateZoneByCodeQueryHandler : IRequestHandler<GetClimateZoneByCodeQuery, ClimateZoneDto?>
{
    private readonly IClimateZoneRepository _repository;

    public GetClimateZoneByCodeQueryHandler(IClimateZoneRepository repository) => _repository = repository;

    public async Task<ClimateZoneDto?> Handle(GetClimateZoneByCodeQuery request, CancellationToken cancellationToken)
    {
        var zone = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        return zone is null ? null : GlassEnclosureMappers.ToDto(zone);
    }
}

public class GetClimateRecommendationQueryHandler : IRequestHandler<GetClimateRecommendationQuery, ClimateRecommendationDto>
{
    private readonly IClimateAdvisor _advisor;

    public GetClimateRecommendationQueryHandler(IClimateAdvisor advisor) => _advisor = advisor;

    public Task<ClimateRecommendationDto> Handle(GetClimateRecommendationQuery request, CancellationToken cancellationToken) =>
        _advisor.RecommendAsync(request.City, request.PostalCode, cancellationToken);
}
