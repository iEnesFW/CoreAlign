using System.Net.Http.Headers;

namespace CoreAlign.Integration.Tests.Infrastructure;

internal static class TestHttpClientExtensions
{
    public static HttpClient AuthenticatedAs(
        this HttpClient client,
        TenantFixture tenant,
        TestPersona persona)
    {
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TenantIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.PersonaHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.RolesHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.EmailHeader);

        var (userId, email, personaValue, roles) = persona switch
        {
            TestPersona.TenantAdmin => (tenant.TenantAdminUserId, tenant.TenantAdminEmail, "tenant", "TenantAdmin"),
            TestPersona.Customer => (tenant.CustomerUserId, tenant.CustomerUserEmail, "customer", "User"),
            TestPersona.Dealer => (tenant.DealerUserId, tenant.DealerUserEmail, "dealer", "User"),
            _ => throw new ArgumentOutOfRangeException(nameof(persona), persona, "Unknown persona"),
        };

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TenantIdHeader, tenant.TenantId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.PersonaHeader, personaValue);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, roles);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}

public enum TestPersona
{
    TenantAdmin,
    Customer,
    Dealer,
}
