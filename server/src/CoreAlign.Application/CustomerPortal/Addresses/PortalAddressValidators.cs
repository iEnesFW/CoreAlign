using FluentValidation;

namespace CoreAlign.Application.CustomerPortal.Addresses;

public class CreatePortalAddressCommandValidator : AbstractValidator<CreatePortalAddressCommand>
{
    public CreatePortalAddressCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(80);
        RuleFor(x => x.State).MaximumLength(80);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(80);
    }
}

public class UpdatePortalAddressCommandValidator : AbstractValidator<UpdatePortalAddressCommand>
{
    public UpdatePortalAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(80);
        RuleFor(x => x.State).MaximumLength(80);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(80);
    }
}

public class DeletePortalAddressCommandValidator : AbstractValidator<DeletePortalAddressCommand>
{
    public DeletePortalAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
