using CoreAlign.Application.GlassPlates.Commands;
using FluentValidation;

namespace CoreAlign.Application.GlassPlates.Validators;

public class CreateStorageLocationValidator : AbstractValidator<CreateStorageLocationCommand>
{
    public CreateStorageLocationValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateStorageLocationValidator : AbstractValidator<UpdateStorageLocationCommand>
{
    public UpdateStorageLocationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
