using FluentValidation;

namespace CoreAlign.Application.B2B;

public class InviteCustomerUserCommandValidator : AbstractValidator<InviteCustomerUserCommand>
{
    public InviteCustomerUserCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public class UpdateCustomerUserStatusCommandValidator : AbstractValidator<UpdateCustomerUserStatusCommand>
{
    public UpdateCustomerUserStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

public class CreateDealerAccountCommandValidator : AbstractValidator<CreateDealerAccountCommand>
{
    public CreateDealerAccountCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(256)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Address).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateDealerAccountCommandValidator : AbstractValidator<UpdateDealerAccountCommand>
{
    public UpdateDealerAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(256)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Address).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class InviteDealerUserCommandValidator : AbstractValidator<InviteDealerUserCommand>
{
    public InviteDealerUserCommandValidator()
    {
        RuleFor(x => x.DealerAccountId).NotEqual(Guid.Empty);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public class UpdateDealerUserStatusCommandValidator : AbstractValidator<UpdateDealerUserStatusCommand>
{
    public UpdateDealerUserStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

public class LinkDealerToCustomerCommandValidator : AbstractValidator<LinkDealerToCustomerCommand>
{
    public LinkDealerToCustomerCommandValidator()
    {
        RuleFor(x => x.DealerAccountId).NotEqual(Guid.Empty);
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UnlinkDealerFromCustomerCommandValidator : AbstractValidator<UnlinkDealerFromCustomerCommand>
{
    public UnlinkDealerFromCustomerCommandValidator()
    {
        RuleFor(x => x.LinkId).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

public class ListDealerUsersQueryValidator : AbstractValidator<ListDealerUsersQuery>
{
    public ListDealerUsersQueryValidator()
    {
        RuleFor(x => x.DealerAccountId).NotEqual(Guid.Empty);
    }
}

public class RejectDealerOrderCommandValidator : AbstractValidator<RejectDealerOrderCommand>
{
    public RejectDealerOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ApproveDealerOrderCommandValidator : AbstractValidator<ApproveDealerOrderCommand>
{
    public ApproveDealerOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty);
    }
}
