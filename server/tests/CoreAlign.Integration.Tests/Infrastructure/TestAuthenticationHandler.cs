using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Integration.Tests.Infrastructure;

public sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "Test";
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationOptions>
{
    public const string UserIdHeader = "X-Test-User-Id";
    public const string TenantIdHeader = "X-Test-Tenant-Id";
    public const string PersonaHeader = "X-Test-Persona";
    public const string RolesHeader = "X-Test-Roles";
    public const string EmailHeader = "X-Test-Email";

    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var request = Context.Request;
        if (!request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdValues.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var tenantId = request.Headers.TryGetValue(TenantIdHeader, out var t) ? t.ToString() : string.Empty;
        var persona = request.Headers.TryGetValue(PersonaHeader, out var p) ? p.ToString() : string.Empty;
        var email = request.Headers.TryGetValue(EmailHeader, out var e) ? e.ToString() : $"{userId}@test.local";
        var rolesRaw = request.Headers.TryGetValue(RolesHeader, out var r) ? r.ToString() : string.Empty;

        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
        };

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId));
        }

        if (!string.IsNullOrWhiteSpace(persona))
        {
            claims.Add(new Claim("persona", persona));
        }

        if (!string.IsNullOrWhiteSpace(rolesRaw))
        {
            foreach (var role in rolesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, TestAuthenticationOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthenticationOptions.SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
