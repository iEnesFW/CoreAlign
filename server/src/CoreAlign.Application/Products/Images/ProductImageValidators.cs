using FluentValidation;

namespace CoreAlign.Application.Products.Images;

public sealed class UploadProductImageValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ProductImagePolicy.IsAllowedContentType)
            .WithMessage("Only JPG, PNG, or WebP images are allowed.");
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(ProductImagePolicy.MaxBytesPerImage)
            .WithMessage($"Image must not exceed {ProductImagePolicy.MaxBytesPerImage / (1024 * 1024)} MB.");
    }
}

public sealed class UpdateProductImageValidator : AbstractValidator<UpdateProductImageCommand>
{
    public UpdateProductImageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
        RuleFor(x => x.AltText).MaximumLength(256);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).LessThan(1000);
    }
}

public sealed class DeleteProductImageValidator : AbstractValidator<DeleteProductImageCommand>
{
    public DeleteProductImageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
