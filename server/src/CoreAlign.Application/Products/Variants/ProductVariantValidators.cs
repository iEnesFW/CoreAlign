using FluentValidation;

namespace CoreAlign.Application.Products.Variants;

public sealed class CreateProductVariantValidator : AbstractValidator<CreateProductVariantCommand>
{
    public CreateProductVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Barcode).MaximumLength(64);
        RuleFor(x => x.VariantAttributesJson)
            .NotEmpty()
            .MaximumLength(4000)
            .Must(BeJsonObject)
            .WithMessage("VariantAttributesJson must be a JSON object.");
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0m).When(x => x.PriceOverride.HasValue);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0m);
    }

    private static bool BeJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            return doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}

public sealed class UpdateProductVariantValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Barcode).MaximumLength(64);
        RuleFor(x => x.VariantAttributesJson)
            .NotEmpty()
            .MaximumLength(4000);
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0m).When(x => x.PriceOverride.HasValue);
    }
}

public sealed class DeleteProductVariantValidator : AbstractValidator<DeleteProductVariantCommand>
{
    public DeleteProductVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
    }
}
