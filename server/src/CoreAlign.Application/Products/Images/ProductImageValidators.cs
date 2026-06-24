using CoreAlign.Application.Common.Upload;
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
            .Must(ct => FileUploadProfiles.ProductImage.AllowedContentTypes.Contains(FileUploadValidator.NormalizeContentType(ct)))
            .WithMessage("Only JPG, PNG, or WebP images are allowed.");
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(FileUploadProfiles.ProductImage.MaxBytes)
            .WithMessage($"Image must not exceed {FileUploadProfiles.ProductImage.MaxBytes / (1024 * 1024)} MB.");
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
