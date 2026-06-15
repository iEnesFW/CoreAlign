using CoreAlign.Application.Sso;
using CoreAlign.Domain.Entities.Sso;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace CoreAlign.Infrastructure.Sso;

public class TenantIdentityProviderService : ITenantIdentityProviderService
{
    private const string SecretProtectorPurpose = "CoreAlign.Sso.ClientSecret.v1";

    private readonly ITenantIdentityProviderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _clock;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IOidcDiscoveryClient _discoveryClient;
    private readonly ISamlMetadataClient _metadataClient;

    public TenantIdentityProviderService(
        ITenantIdentityProviderRepository repository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IDateTimeProvider clock,
        IDataProtectionProvider dataProtectionProvider,
        IOidcDiscoveryClient discoveryClient,
        ISamlMetadataClient metadataClient)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _clock = clock;
        _dataProtectionProvider = dataProtectionProvider;
        _discoveryClient = discoveryClient;
        _metadataClient = metadataClient;
    }

    public async Task<IReadOnlyList<SsoIdentityProviderDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var entities = await _repository.ListByTenantAsync(tenantId, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<SsoIdentityProviderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;
        _tenantContext.EnsureSameTenant(entity.TenantId);
        return MapToDto(entity);
    }

    public async Task<SsoIdentityProviderDto> CreateAsync(CreateSsoIdentityProviderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = _tenantContext.RequireTenantId();

        var existing = await _repository.GetByTenantAndNameAsync(tenantId, request.Name.Trim(), cancellationToken);
        if (existing is not null) throw new SsoProviderDuplicateException();

        var encryptedSecret = ProtectSecret(request.ClientSecret);
        var entity = TenantIdentityProvider.Create(
            tenantId,
            request.Name,
            request.Protocol,
            request.EntityIdOrClientId,
            request.MetadataUrl,
            request.DiscoveryDocumentUrl,
            encryptedSecret,
            request.AttributeMappingsJson);

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<SsoIdentityProviderDto> UpdateAsync(Guid id, UpdateSsoIdentityProviderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new SsoProviderNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);

        var encryptedSecret = ProtectSecret(request.ClientSecret);
        entity.Update(
            request.Name,
            request.EntityIdOrClientId,
            request.MetadataUrl,
            request.DiscoveryDocumentUrl,
            encryptedSecret,
            request.AttributeMappingsJson,
            request.IsActive,
            _clock.UtcNow);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new SsoProviderNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);
        entity.MarkDeleted(null, "Removed via Admin API", _clock.UtcNow);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SsoTestConnectionResult> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new SsoProviderNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);

        if (entity.Protocol == SsoProtocol.Saml)
        {
            if (string.IsNullOrWhiteSpace(entity.MetadataUrl))
            {
                return new SsoTestConnectionResult(false, "SAML metadata URL is empty.");
            }
            var ok = await _metadataClient.ValidateMetadataAsync(entity.MetadataUrl, cancellationToken);
            return new SsoTestConnectionResult(ok, ok ? "SAML metadata reachable." : "SAML metadata fetch failed.");
        }

        if (string.IsNullOrWhiteSpace(entity.DiscoveryDocumentUrl))
        {
            return new SsoTestConnectionResult(false, "OIDC discovery URL is empty.");
        }
        var doc = await _discoveryClient.FetchAsync(entity.DiscoveryDocumentUrl, cancellationToken);
        return doc is null
            ? new SsoTestConnectionResult(false, "OIDC discovery fetch failed.")
            : new SsoTestConnectionResult(true, $"OIDC issuer: {doc.Issuer}");
    }

    private string? ProtectSecret(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) return null;
        var protector = _dataProtectionProvider.CreateProtector(SecretProtectorPurpose);
        return protector.Protect(plaintext);
    }

    private static SsoIdentityProviderDto MapToDto(TenantIdentityProvider e) =>
        new(
            e.Id,
            e.TenantId,
            e.Name,
            e.Protocol,
            e.EntityIdOrClientId,
            e.MetadataUrl,
            e.DiscoveryDocumentUrl,
            e.AttributeMappingsJson,
            e.IsActive,
            e.LastUsedAtUtc,
            e.CreatedAtUtc,
            e.UpdatedAtUtc);
}
