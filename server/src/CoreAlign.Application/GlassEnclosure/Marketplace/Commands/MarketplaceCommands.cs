using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Commands;

public record SubmitToMarketplaceCommand(Guid TenantTemplateId)
    : IRequest<MarketplaceSubmissionDto>, ITransactionalRequest;

public record PublishMarketplaceCommand(Guid TemplateId)
    : IRequest<MarketplaceSubmissionDto>, ITransactionalRequest;

public record RejectMarketplaceCommand(Guid TemplateId, string Reason)
    : IRequest<MarketplaceSubmissionDto>, ITransactionalRequest;

public record InstallMarketplaceTemplateCommand(Guid MarketplaceTemplateId)
    : IRequest<InstallMarketplaceResultDto>, ITransactionalRequest;

public record RateMarketplaceTemplateCommand(Guid TemplateId, int RatingStars, string? CommentMd)
    : IRequest<MarketplaceReviewDto>, ITransactionalRequest;
