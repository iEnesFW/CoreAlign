using FluentValidation;

namespace CoreAlign.Application.Inventory.Serials;

public class RegisterSerialUnitsCommandValidator : AbstractValidator<RegisterSerialUnitsCommand>
{
    public RegisterSerialUnitsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SerialNumbers).NotEmpty().WithMessage("At least one serial number is required.");
        RuleForEach(x => x.SerialNumbers).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0m);
    }
}

public class ShipSerialUnitsCommandValidator : AbstractValidator<ShipSerialUnitsCommand>
{
    public ShipSerialUnitsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.SerialNumbers).NotEmpty().WithMessage("At least one serial number is required.");
        RuleForEach(x => x.SerialNumbers).NotEmpty().MaximumLength(100);
    }
}
