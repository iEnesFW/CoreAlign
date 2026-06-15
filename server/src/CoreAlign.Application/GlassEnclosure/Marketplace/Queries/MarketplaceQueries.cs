using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Queries;

public record ListMarketplaceTemplatesQuery(
    EnclosureCategory? Category,
    MarketplaceSortBy SortBy,
    int Skip,
    int Take)
    : IRequest<IReadOnlyList<MarketplaceTemplateSummaryDto>>;

public record GetMarketplaceTemplateByIdQuery(Guid Id)
    : IRequest<MarketplaceTemplateDetailDto?>;

public record ListMyMarketplaceSubmissionsQuery
    : IRequest<IReadOnlyList<MarketplaceSubmissionDto>>;

public record ListPendingMarketplaceSubmissionsQuery
    : IRequest<IReadOnlyList<MarketplaceSubmissionDto>>;

public record ListMarketplaceReviewsQuery(Guid TemplateId, int Skip, int Take)
    : IRequest<IReadOnlyList<MarketplaceReviewDto>>;
