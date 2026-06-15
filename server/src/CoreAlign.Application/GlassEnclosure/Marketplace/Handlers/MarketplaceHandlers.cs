using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Marketplace.Commands;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Application.GlassEnclosure.Marketplace.Queries;
using CoreAlign.Application.GlassEnclosure.Marketplace.Services;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Handlers;

public class SubmitToMarketplaceCommandHandler : IRequestHandler<SubmitToMarketplaceCommand, MarketplaceSubmissionDto>
{
    private readonly IProjectMarketplaceService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public SubmitToMarketplaceCommandHandler(IProjectMarketplaceService service, ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public Task<MarketplaceSubmissionDto> Handle(SubmitToMarketplaceCommand request, CancellationToken cancellationToken) =>
        _service.SubmitToMarketplaceAsync(request.TenantTemplateId, _currentUser.UserIdOrThrow(), cancellationToken);
}

public class PublishMarketplaceCommandHandler : IRequestHandler<PublishMarketplaceCommand, MarketplaceSubmissionDto>
{
    private readonly IProjectMarketplaceService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public PublishMarketplaceCommandHandler(IProjectMarketplaceService service, ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public Task<MarketplaceSubmissionDto> Handle(PublishMarketplaceCommand request, CancellationToken cancellationToken) =>
        _service.PublishAsync(request.TemplateId, _currentUser.UserIdOrThrow(), cancellationToken);
}

public class RejectMarketplaceCommandHandler : IRequestHandler<RejectMarketplaceCommand, MarketplaceSubmissionDto>
{
    private readonly IProjectMarketplaceService _service;
    public RejectMarketplaceCommandHandler(IProjectMarketplaceService service) => _service = service;

    public Task<MarketplaceSubmissionDto> Handle(RejectMarketplaceCommand request, CancellationToken cancellationToken) =>
        _service.RejectAsync(request.TemplateId, request.Reason, cancellationToken);
}

public class InstallMarketplaceTemplateCommandHandler : IRequestHandler<InstallMarketplaceTemplateCommand, InstallMarketplaceResultDto>
{
    private readonly IProjectMarketplaceService _service;
    public InstallMarketplaceTemplateCommandHandler(IProjectMarketplaceService service) => _service = service;

    public Task<InstallMarketplaceResultDto> Handle(InstallMarketplaceTemplateCommand request, CancellationToken cancellationToken) =>
        _service.InstallToTenantAsync(request.MarketplaceTemplateId, cancellationToken);
}

public class RateMarketplaceTemplateCommandHandler : IRequestHandler<RateMarketplaceTemplateCommand, MarketplaceReviewDto>
{
    private readonly IProjectMarketplaceService _service;
    public RateMarketplaceTemplateCommandHandler(IProjectMarketplaceService service) => _service = service;

    public Task<MarketplaceReviewDto> Handle(RateMarketplaceTemplateCommand request, CancellationToken cancellationToken) =>
        _service.RateAsync(request.TemplateId, request.RatingStars, request.CommentMd, cancellationToken);
}

public class ListMarketplaceTemplatesQueryHandler
    : IRequestHandler<ListMarketplaceTemplatesQuery, IReadOnlyList<MarketplaceTemplateSummaryDto>>
{
    private readonly IProjectMarketplaceService _service;
    public ListMarketplaceTemplatesQueryHandler(IProjectMarketplaceService service) => _service = service;

    public Task<IReadOnlyList<MarketplaceTemplateSummaryDto>> Handle(ListMarketplaceTemplatesQuery request, CancellationToken cancellationToken) =>
        _service.ListMarketplaceAsync(request.Category, request.SortBy, request.Skip, request.Take, cancellationToken);
}

public class GetMarketplaceTemplateByIdQueryHandler
    : IRequestHandler<GetMarketplaceTemplateByIdQuery, MarketplaceTemplateDetailDto?>
{
    private readonly IProjectMarketplaceService _service;
    public GetMarketplaceTemplateByIdQueryHandler(IProjectMarketplaceService service) => _service = service;

    public Task<MarketplaceTemplateDetailDto?> Handle(GetMarketplaceTemplateByIdQuery request, CancellationToken cancellationToken) =>
        _service.GetMarketplaceTemplateAsync(request.Id, cancellationToken);
}

public class ListMyMarketplaceSubmissionsQueryHandler
    : IRequestHandler<ListMyMarketplaceSubmissionsQuery, IReadOnlyList<MarketplaceSubmissionDto>>
{
    private readonly IProjectMarketplaceService _service;
    public ListMyMarketplaceSubmissionsQueryHandler(IProjectMarketplaceService service) => _service = service;

    public Task<IReadOnlyList<MarketplaceSubmissionDto>> Handle(ListMyMarketplaceSubmissionsQuery request, CancellationToken cancellationToken) =>
        _service.ListMyTenantSubmissionsAsync(cancellationToken);
}

public class ListPendingMarketplaceSubmissionsQueryHandler
    : IRequestHandler<ListPendingMarketplaceSubmissionsQuery, IReadOnlyList<MarketplaceSubmissionDto>>
{
    private readonly IProjectMarketplaceService _service;
    public ListPendingMarketplaceSubmissionsQueryHandler(IProjectMarketplaceService service) => _service = service;

    public Task<IReadOnlyList<MarketplaceSubmissionDto>> Handle(ListPendingMarketplaceSubmissionsQuery request, CancellationToken cancellationToken) =>
        _service.ListPendingSubmissionsAsync(cancellationToken);
}

public class ListMarketplaceReviewsQueryHandler
    : IRequestHandler<ListMarketplaceReviewsQuery, IReadOnlyList<MarketplaceReviewDto>>
{
    private readonly IProjectMarketplaceService _service;
    public ListMarketplaceReviewsQueryHandler(IProjectMarketplaceService service) => _service = service;

    public Task<IReadOnlyList<MarketplaceReviewDto>> Handle(ListMarketplaceReviewsQuery request, CancellationToken cancellationToken) =>
        _service.ListReviewsAsync(request.TemplateId, request.Skip, request.Take, cancellationToken);
}
