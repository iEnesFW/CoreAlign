using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Queries;

public record ListProjectTemplatesQuery(EnclosureCategory? Category)
    : IRequest<IReadOnlyList<ProjectTemplateSummaryDto>>;

public record GetProjectTemplateByIdQuery(Guid Id)
    : IRequest<ProjectTemplateDetailDto?>;
