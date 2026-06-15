using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Queries;

public record GetWindZonesQuery(bool? IsActive = true) : IRequest<IReadOnlyList<WindZoneDto>>;

public record GetWindZoneByCodeQuery(string Code) : IRequest<WindZoneDto?>;

public record GetClimateZonesQuery(bool? IsActive = true) : IRequest<IReadOnlyList<ClimateZoneDto>>;

public record GetClimateZoneByCodeQuery(string Code) : IRequest<ClimateZoneDto?>;

public record GetClimateRecommendationQuery(string? City, string? PostalCode) : IRequest<ClimateRecommendationDto>;
