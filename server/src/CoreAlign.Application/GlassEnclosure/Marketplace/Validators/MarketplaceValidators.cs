using CoreAlign.Application.GlassEnclosure.Marketplace.Commands;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Validators;

public class SubmitToMarketplaceCommandValidator : AbstractValidator<SubmitToMarketplaceCommand>
{
    public SubmitToMarketplaceCommandValidator()
    {
        RuleFor(x => x.TenantTemplateId).NotEmpty();
    }
}

public class PublishMarketplaceCommandValidator : AbstractValidator<PublishMarketplaceCommand>
{
    public PublishMarketplaceCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}

public class RejectMarketplaceCommandValidator : AbstractValidator<RejectMarketplaceCommand>
{
    public RejectMarketplaceCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class InstallMarketplaceTemplateCommandValidator : AbstractValidator<InstallMarketplaceTemplateCommand>
{
    public InstallMarketplaceTemplateCommandValidator()
    {
        RuleFor(x => x.MarketplaceTemplateId).NotEmpty();
    }
}

public class RateMarketplaceTemplateCommandValidator : AbstractValidator<RateMarketplaceTemplateCommand>
{
    public RateMarketplaceTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.RatingStars).InclusiveBetween(1, 5);
        RuleFor(x => x.CommentMd).MaximumLength(4000);
    }
}
