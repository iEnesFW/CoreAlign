using CoreAlign.Application.GlassEnclosure.DTOs;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface IClimateAdvisor
{
    Task<ClimateRecommendationDto> RecommendAsync(
        string? city,
        string? postalCode,
        CancellationToken cancellationToken = default);
}
