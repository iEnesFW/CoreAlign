using FluentValidation;

namespace CoreAlign.Application.Tenants.Logo;

public sealed class UploadTenantLogoValidator : AbstractValidator<UploadTenantLogoCommand>
{
    public UploadTenantLogoValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(TenantLogoPolicy.IsAllowedContentType)
            .WithMessage("Only PNG, JPG, or SVG logos are allowed.");
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(TenantLogoPolicy.MaxBytes)
            .WithMessage($"Logo must not exceed {TenantLogoPolicy.MaxBytes / 1024} KB.");
    }
}
