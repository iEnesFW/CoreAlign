using CoreAlign.Application.Inventory.Commands;
using FluentValidation;

namespace CoreAlign.Application.Inventory.Validators;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0m).WithMessage("Validation.DeltaCannotBeZero");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0m).WithMessage("Validation.NonNegative");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class IssueStockCommandValidator : AbstractValidator<IssueStockCommand>
{
    public IssueStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateLotCommandValidator : AbstractValidator<CreateLotCommand>
{
    public CreateLotCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LotNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SupplierLotRef).MaximumLength(64);
        RuleFor(x => x.CountryOfOrigin).MaximumLength(3);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateLotCommandValidator : AbstractValidator<UpdateLotCommand>
{
    public UpdateLotCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LotNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SupplierLotRef).MaximumLength(64);
        RuleFor(x => x.CountryOfOrigin).MaximumLength(3);
        RuleFor(x => x.BlockReason).MaximumLength(500);
    }
}
