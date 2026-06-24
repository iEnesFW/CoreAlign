using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tenants.Logo;

public sealed class UploadTenantLogoHandler : IRequestHandler<UploadTenantLogoCommand, TenantLogoDto>
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;
    private readonly IFileUploadService _uploads;
    private readonly IUnitOfWork _uow;

    public UploadTenantLogoHandler(
        ITenantRepository tenants,
        ITenantContext tenantContext,
        IFileUploadService uploads,
        IUnitOfWork uow)
    {
        _tenants = tenants;
        _tenantContext = tenantContext;
        _uploads = uploads;
        _uow = uow;
    }

    public async Task<TenantLogoDto> Handle(UploadTenantLogoCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new TenantNotFoundException();

        var uploaded = await _uploads.UploadAsync(
            new FileUploadRequest(
                request.Content,
                request.FileName,
                request.ContentType,
                FileUploadProfiles.TenantLogo.Name,
                TenantLogoPolicy.StorageScope),
            cancellationToken);

        tenant.LogoUrl = uploaded.PublicUrl;
        tenant.UpdatedAtUtc = DateTime.UtcNow;
        _tenants.Update(tenant);
        await _uow.SaveChangesAsync(cancellationToken);

        return new TenantLogoDto(uploaded.PublicUrl, uploaded.RelativePath, uploaded.ContentType, uploaded.SizeBytes);
    }
}
