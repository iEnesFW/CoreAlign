using CoreAlign.Application.Sales.OrderTemplates.Commands;
using FluentValidation;

namespace CoreAlign.Application.Sales.OrderTemplates.Validators;

public class CreateOrderTemplateCommandValidator : AbstractValidator<CreateOrderTemplateCommand>
{
    public CreateOrderTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.OrderTemplateNoLines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0m);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0m);
        });
    }
}

public class UpdateOrderTemplateCommandValidator : AbstractValidator<UpdateOrderTemplateCommand>
{
    public UpdateOrderTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0m);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0m);
        });
    }
}

public class DeleteOrderTemplateCommandValidator : AbstractValidator<DeleteOrderTemplateCommand>
{
    public DeleteOrderTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class SetOrderTemplateActiveCommandValidator : AbstractValidator<SetOrderTemplateActiveCommand>
{
    public SetOrderTemplateActiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class RunOrderTemplateNowCommandValidator : AbstractValidator<RunOrderTemplateNowCommand>
{
    public RunOrderTemplateNowCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
