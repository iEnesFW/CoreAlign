using CoreAlign.Application.Common.Upload;
using FluentValidation;

namespace CoreAlign.Application.Tenants.Logo;

public sealed class UploadTenantLogoValidator : AbstractValidator<UploadTenantLogoCommand>
{
    public UploadTenantLogoValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => FileUploadProfiles.TenantLogo.AllowedContentTypes.Contains(FileUploadValidator.NormalizeContentType(ct)))
            .WithMessage("Only PNG, JPG, or SVG logos are allowed.");
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(FileUploadProfiles.TenantLogo.MaxBytes)
            .WithMessage($"Logo must not exceed {FileUploadProfiles.TenantLogo.MaxBytes / 1024} KB.");
    }
}
