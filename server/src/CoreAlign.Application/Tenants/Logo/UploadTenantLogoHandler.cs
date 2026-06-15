using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tenants.Logo;

public sealed class UploadTenantLogoHandler : IRequestHandler<UploadTenantLogoCommand, TenantLogoDto>
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public UploadTenantLogoHandler(
        ITenantRepository tenants,
        ITenantContext tenantContext,
        IFileStorage storage,
        IUnitOfWork uow)
    {
        _tenants = tenants;
        _tenantContext = tenantContext;
        _storage = storage;
        _uow = uow;
    }

    public async Task<TenantLogoDto> Handle(UploadTenantLogoCommand request, CancellationToken cancellationToken)
    {
        if (!TenantLogoPolicy.IsAllowedContentType(request.ContentType))
        {
            throw new ArgumentException("Only PNG, JPG, or SVG logos are allowed.", nameof(request));
        }

        if (!TenantLogoPolicy.IsAllowedExtension(request.FileName))
        {
            throw new ArgumentException("File name must end with .png, .jpg, .jpeg, or .svg.", nameof(request));
        }

        if (!TenantLogoPolicy.MatchesContentTypeAndExtension(request.ContentType, request.FileName))
        {
            throw new ArgumentException("File extension does not match the declared image type.", nameof(request));
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > TenantLogoPolicy.MaxBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"Logo must be between 1 byte and {TenantLogoPolicy.MaxBytes} bytes.");
        }

        if (!await TenantLogoPolicy.LooksLikeLogoAsync(request.Content, request.ContentType, cancellationToken))
        {
            throw new ArgumentException("File content does not match a supported logo format.", nameof(request));
        }

        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new TenantNotFoundException();

        var stored = await _storage.SaveAsync(
            TenantLogoPolicy.StorageScope,
            request.FileName,
            request.Content,
            request.ContentType,
            cancellationToken);

        tenant.LogoUrl = stored.PublicUrl;
        tenant.UpdatedAtUtc = DateTime.UtcNow;
        _tenants.Update(tenant);
        await _uow.SaveChangesAsync(cancellationToken);

        return new TenantLogoDto(stored.PublicUrl, stored.RelativePath, stored.ContentType, stored.SizeBytes);
    }
}
