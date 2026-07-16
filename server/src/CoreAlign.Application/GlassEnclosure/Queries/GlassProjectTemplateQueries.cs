using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Queries;

public record GetMyGlassProjectTemplatesQuery()
    : IRequest<IReadOnlyList<GlassProjectTemplateSummaryDto>>;

public record GetGlassProjectTemplateByIdQuery(Guid Id)
    : IRequest<GlassProjectTemplateDto?>;
