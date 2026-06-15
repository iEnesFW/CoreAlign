using CoreAlign.Application.GlassEnclosure.Services;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Validators;

/// <summary>
/// Future-proof validator for <see cref="BOMLineDraft"/> ensuring that every non-service line
/// resolves to a canonical <c>ProductId</c>. Service lines (labor, transport, installation, etc.)
/// are exempt from the product link requirement because they do not generate stock movements.
/// </summary>
public class BomLineDraftValidator : AbstractValidator<BOMLineDraft>
{
    public BomLineDraftValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Quantity)
            .GreaterThan(0m);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        When(x => !x.IsService, () =>
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("BomLine.ProductId.Required");
        });
    }
}
