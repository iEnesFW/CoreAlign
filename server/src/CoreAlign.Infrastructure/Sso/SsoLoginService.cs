using System.Globalization;
using System.Web;
using CoreAlign.Application.Sso;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sso;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Sso;

public class SsoLoginService : ISsoLoginService
{
    private readonly ITenantIdentityProviderRepository _idpRepository;
    private readonly IExternalUserBindingRepository _bindingRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ILoginAuditLogRepository _loginAuditLogRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOidcDiscoveryClient _discoveryClient;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SsoLoginService> _logger;

    public SsoLoginService(
        ITenantIdentityProviderRepository idpRepository,
        IExternalUserBindingRepository bindingRepository,
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository userSessionRepository,
        ILoginAuditLogRepository loginAuditLogRepository,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOidcDiscoveryClient discoveryClient,
        ITenantContext tenantContext,
        ILogger<SsoLoginService> logger)
    {
        _idpRepository = idpRepository;
        _bindingRepository = bindingRepository;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userSessionRepository = userSessionRepository;
        _loginAuditLogRepository = loginAuditLogRepository;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _discoveryClient = discoveryClient;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public Task<SsoLoginResult> CompleteSamlLoginAsync(string tenantSlug, string idpName, SsoAssertionContext assertion, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) =>
        CompleteSsoLoginInternalAsync(tenantSlug, idpName, SsoProtocol.Saml, assertion, ipAddress, userAgent, cancellationToken);

    public Task<SsoLoginResult> CompleteOidcLoginAsync(string tenantSlug, string idpName, SsoAssertionContext assertion, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default) =>
        CompleteSsoLoginInternalAsync(tenantSlug, idpName, SsoProtocol.Oidc, assertion, ipAddress, userAgent, cancellationToken);

    public async Task<string> BuildSamlRedirectUrlAsync(string tenantSlug, string idpName, string returnUrl, CancellationToken cancellationToken = default)
    {
        var (_, idp) = await ResolveTenantAndProviderAsync(tenantSlug, idpName, SsoProtocol.Saml, cancellationToken);
        if (string.IsNullOrWhiteSpace(idp.MetadataUrl))
        {
            throw new SsoAssertionInvalidException("SAML provider missing metadata URL.");
        }
        var encodedReturn = HttpUtility.UrlEncode(returnUrl);
        return $"{idp.MetadataUrl}?SAMLRequest=&RelayState={encodedReturn}";
    }

    public async Task<string> BuildOidcAuthorizeUrlAsync(string tenantSlug, string idpName, string returnUrl, string state, CancellationToken cancellationToken = default)
    {
        var (_, idp) = await ResolveTenantAndProviderAsync(tenantSlug, idpName, SsoProtocol.Oidc, cancellationToken);
        if (string.IsNullOrWhiteSpace(idp.DiscoveryDocumentUrl))
        {
            throw new SsoAssertionInvalidException("OIDC provider missing discovery document URL.");
        }
        var discovery = await _discoveryClient.FetchAsync(idp.DiscoveryDocumentUrl, cancellationToken)
            ?? throw new SsoAssertionInvalidException("OIDC discovery fetch failed.");

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["response_type"] = "code";
        query["client_id"] = idp.EntityIdOrClientId;
        query["redirect_uri"] = returnUrl;
        query["scope"] = "openid email profile";
        query["state"] = state;
        return $"{discovery.AuthorizationEndpoint}?{query}";
    }

    private async Task<SsoLoginResult> CompleteSsoLoginInternalAsync(
        string tenantSlug,
        string idpName,
        SsoProtocol expectedProtocol,
        SsoAssertionContext assertion,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        if (string.IsNullOrWhiteSpace(assertion.ExternalUserId))
        {
            throw new SsoAssertionInvalidException("External user identifier missing from assertion.");
        }
        if (string.IsNullOrWhiteSpace(assertion.Email))
        {
            throw new SsoAssertionInvalidException("Email claim missing from assertion.");
        }

        var (tenant, idp) = await ResolveTenantAndProviderAsync(tenantSlug, idpName, expectedProtocol, cancellationToken);

        using (_tenantContext.PushScope(tenant.Id))
        {
            var binding = await _bindingRepository.GetByExternalIdAsync(idp.Id, assertion.ExternalUserId, cancellationToken);

            User? user;
            if (binding is null)
            {
                user = await _userRepository.GetByEmailAsync(assertion.Email, cancellationToken);
                if (user is null || user.TenantId != tenant.Id)
                {
                    throw new SsoAssertionInvalidException("No local user matches the SSO assertion.");
                }
                var newBinding = ExternalUserBinding.Create(tenant.Id, user.Id, idp.Id, assertion.ExternalUserId, assertion.Email);
                newBinding.RecordLogin(_clock.UtcNow);
                await _bindingRepository.AddAsync(newBinding, cancellationToken);
            }
            else
            {
                user = await _userRepository.GetByIdAsync(binding.LocalUserId, cancellationToken)
                    ?? throw new UserNotFoundException();
                binding.RecordLogin(_clock.UtcNow);
                _bindingRepository.Update(binding);
            }

            if (!user.IsActive) throw new AccountDisabledException();

            idp.RecordUsage(_clock.UtcNow);
            _idpRepository.Update(idp);

            user.RecordSuccessfulLogin();
            _userRepository.Update(user);

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email, roles);
            var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _jwtTokenService.HashToken(rawRefreshToken);

            var refreshToken = new RefreshToken(
                user.Id,
                refreshTokenHash,
                _clock.UtcNow.AddDays(7),
                userAgent,
                ipAddress);
            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            var session = new UserSession(
                user.Id,
                refreshTokenHash,
                _clock.UtcNow.AddDays(7),
                userAgent,
                ipAddress);
            await _userSessionRepository.AddAsync(session, cancellationToken);

            var auditDescription = string.Format(CultureInfo.InvariantCulture, "SSO-{0}:{1}", expectedProtocol, idp.Name);
            var successLog = new LoginAuditLog(
                user.Email,
                LoginResultType.Success,
                user.Id,
                ipAddress,
                userAgent,
                auditDescription);
            await _loginAuditLogRepository.AddAsync(successLog, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sso.Login.Success protocol={Protocol} tenant={Tenant} idp={Idp} user={UserId}",
                expectedProtocol, tenant.Id, idp.Id, user.Id);

            return new SsoLoginResult(
                accessToken,
                rawRefreshToken,
                _clock.UtcNow.AddMinutes(15),
                user.Id,
                user.TenantId,
                user.Email,
                roles);
        }
    }

    private async Task<(Tenant Tenant, TenantIdentityProvider Idp)> ResolveTenantAndProviderAsync(
        string tenantSlug,
        string idpName,
        SsoProtocol expectedProtocol,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug)) throw new SsoAssertionInvalidException("Tenant slug missing.");
        if (string.IsNullOrWhiteSpace(idpName)) throw new SsoAssertionInvalidException("Identity provider name missing.");

        var tenant = await _tenantRepository.GetBySlugAsync(tenantSlug, cancellationToken)
            ?? throw new TenantNotFoundException();

        var idp = await _idpRepository.GetByTenantAndNameAsync(tenant.Id, idpName, cancellationToken)
            ?? throw new SsoProviderNotFoundException();

        if (!idp.IsActive) throw new SsoProviderInactiveException();
        if (idp.Protocol != expectedProtocol)
        {
            throw new SsoAssertionInvalidException($"Identity provider {idpName} is not configured for {expectedProtocol}.");
        }

        return (tenant, idp);
    }
}
