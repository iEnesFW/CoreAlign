using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Application.GlassEnclosure.Services;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class CreateProjectFromTemplateCommandHandler : IRequestHandler<CreateProjectFromTemplateCommand, GlassProjectDto>
{
    private readonly IProjectTemplateService _service;

    public CreateProjectFromTemplateCommandHandler(IProjectTemplateService service) => _service = service;

    public Task<GlassProjectDto> Handle(CreateProjectFromTemplateCommand request, CancellationToken cancellationToken) =>
        _service.CreateProjectFromTemplateAsync(
            request.Data.TemplateId,
            request.Data.CustomerId,
            request.Data.ProjectName,
            request.Data.Currency,
            cancellationToken);
}

public class ListProjectTemplatesQueryHandler : IRequestHandler<ListProjectTemplatesQuery, IReadOnlyList<ProjectTemplateSummaryDto>>
{
    private readonly IProjectTemplateService _service;

    public ListProjectTemplatesQueryHandler(IProjectTemplateService service) => _service = service;

    public Task<IReadOnlyList<ProjectTemplateSummaryDto>> Handle(ListProjectTemplatesQuery request, CancellationToken cancellationToken) =>
        _service.ListAsync(request.Category, cancellationToken);
}

public class GetProjectTemplateByIdQueryHandler : IRequestHandler<GetProjectTemplateByIdQuery, ProjectTemplateDetailDto?>
{
    private readonly IProjectTemplateService _service;

    public GetProjectTemplateByIdQueryHandler(IProjectTemplateService service) => _service = service;

    public Task<ProjectTemplateDetailDto?> Handle(GetProjectTemplateByIdQuery request, CancellationToken cancellationToken) =>
        _service.GetByIdAsync(request.Id, cancellationToken);
}
