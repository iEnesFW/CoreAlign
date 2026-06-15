using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface IProjectTemplateService
{
    Task<IReadOnlyList<ProjectTemplateSummaryDto>> ListAsync(
        EnclosureCategory? category,
        CancellationToken cancellationToken = default);

    Task<ProjectTemplateDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GlassProjectDto> CreateProjectFromTemplateAsync(
        Guid templateId,
        Guid customerId,
        string projectName,
        string? currency,
        CancellationToken cancellationToken = default);
}
