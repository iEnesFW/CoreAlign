using FluentValidation;

namespace CoreAlign.Application.B2B.CustomerPortal;

public class SetDealerProductVisibilityCommandValidator : AbstractValidator<SetDealerProductVisibilityCommand>
{
    public SetDealerProductVisibilityCommandValidator()
    {
        RuleFor(x => x.DealerCustomerLinkId).NotEqual(Guid.Empty);
        RuleFor(x => x.Mode)
            .NotEmpty()
            .Must(m => m == DealerProductVisibilityModes.All || m == DealerProductVisibilityModes.Whitelist)
            .WithMessage("Mode must be 'All' or 'Whitelist'.");
        When(x => x.Mode == DealerProductVisibilityModes.Whitelist, () =>
        {
            RuleFor(x => x.ProductIds)
                .NotNull()
                .Must(ids => ids != null && ids.Count > 0)
                .WithMessage("At least one product id is required when mode is 'Whitelist'.");
            RuleForEach(x => x.ProductIds).NotEqual(Guid.Empty);
        });
    }
}
