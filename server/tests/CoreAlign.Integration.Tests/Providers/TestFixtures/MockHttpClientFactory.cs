using RichardSzalay.MockHttp;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// Lightweight <see cref="IHttpClientFactory"/> used by the integration test
/// harness to bind every named-client lookup to a <see cref="MockHttpMessageHandler"/>
/// so the real provider classes exercise their HTTP pipeline against a deterministic stub.
/// </summary>
public sealed class MockHttpClientFactory : IHttpClientFactory
{
    private readonly MockHttpMessageHandler _handler;
    private readonly Uri? _baseAddress;

    public MockHttpClientFactory(MockHttpMessageHandler handler, Uri? baseAddress = null)
    {
        _handler = handler;
        _baseAddress = baseAddress;
    }

    public HttpClient CreateClient(string name)
    {
        var client = _handler.ToHttpClient();
        if (_baseAddress is not null)
        {
            client.BaseAddress = _baseAddress;
        }
        return client;
    }
}
