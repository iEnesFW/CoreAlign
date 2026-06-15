using System.Net;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Fx;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Fx;

public class EcbFxProviderTests
{
    private static readonly DateTime AsOf = DateTime.SpecifyKind(new DateTime(2026, 6, 4), DateTimeKind.Utc);

    private const string TryXml = """
<?xml version="1.0" encoding="UTF-8" ?>
<message:GenericData xmlns:message="http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message" xmlns:generic="http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic">
  <message:DataSet>
    <generic:Series>
      <generic:SeriesKey>
        <generic:Value id="CURRENCY" value="TRY" />
        <generic:Value id="CURRENCY_DENOM" value="EUR" />
      </generic:SeriesKey>
      <generic:Obs>
        <generic:ObsDimension value="2026-06-04" />
        <generic:ObsValue value="35.0" />
      </generic:Obs>
    </generic:Series>
  </message:DataSet>
</message:GenericData>
""";

    private const string UsdXml = """
<?xml version="1.0" encoding="UTF-8" ?>
<message:GenericData xmlns:message="http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message" xmlns:generic="http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic">
  <message:DataSet>
    <generic:Series>
      <generic:SeriesKey>
        <generic:Value id="CURRENCY" value="USD" />
        <generic:Value id="CURRENCY_DENOM" value="EUR" />
      </generic:SeriesKey>
      <generic:Obs>
        <generic:ObsDimension value="2026-06-04" />
        <generic:ObsValue value="1.08" />
      </generic:Obs>
    </generic:Series>
  </message:DataSet>
</message:GenericData>
""";

    [Fact]
    public async Task TryGetRateAsync_returns_one_for_base_currency_eur()
    {
        var sut = BuildProvider(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty),
        });

        var snapshot = await sut.TryGetRateAsync("EUR", AsOf);

        snapshot.Should().NotBeNull();
        snapshot!.CurrencyCode.Should().Be("EUR");
        snapshot.BuyingRate.Should().Be(1m);
        snapshot.Source.Should().Be(FxSourceCodes.Ecb);
    }

    [Fact]
    public async Task TryGetRateAsync_returns_try_rate_from_inverse_of_eur_per_try()
    {
        var sut = BuildProvider(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(TryXml),
            };
        });

        var snapshot = await sut.TryGetRateAsync("TRY", AsOf);

        snapshot.Should().NotBeNull();
        snapshot!.CurrencyCode.Should().Be("TRY");
        snapshot.BuyingRate.Should().Be(Math.Round(1m / 35.0m, 6, MidpointRounding.ToEven));
        snapshot.Source.Should().Be(FxSourceCodes.Ecb);
    }

    [Fact]
    public async Task TryGetRateAsync_computes_cross_rate_for_target_against_try()
    {
        var sut = BuildProvider(req =>
        {
            var url = req.RequestUri!.ToString();
            var payload = url.Contains("D.TRY.EUR", StringComparison.OrdinalIgnoreCase) ? TryXml : UsdXml;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload),
            };
        });

        var snapshot = await sut.TryGetRateAsync("USD", AsOf);

        snapshot.Should().NotBeNull();
        snapshot!.CurrencyCode.Should().Be("USD");
        var expected = Math.Round(35.0m / 1.08m, 6, MidpointRounding.ToEven);
        snapshot.BuyingRate.Should().Be(expected);
    }

    [Fact]
    public async Task TryGetRateAsync_returns_null_when_unsupported_currency()
    {
        var sut = BuildProvider(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var supports = sut.SupportsCurrency("XAU");

        supports.Should().BeFalse();
    }

    [Fact]
    public async Task TryGetRateAsync_returns_null_when_http_request_fails()
    {
        var sut = BuildProvider(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var snapshot = await sut.TryGetRateAsync("USD", AsOf);

        snapshot.Should().BeNull();
    }

    [Fact]
    public async Task TryGetRateAsync_uses_memory_cache_to_avoid_repeated_http_calls()
    {
        var hits = 0;
        var sut = BuildProvider(req =>
        {
            hits++;
            var payload = req.RequestUri!.ToString().Contains("D.TRY.EUR", StringComparison.OrdinalIgnoreCase)
                ? TryXml : UsdXml;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload),
            };
        });

        var first = await sut.TryGetRateAsync("USD", AsOf);
        var second = await sut.TryGetRateAsync("USD", AsOf);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        hits.Should().Be(2, "first call fetches TRY + USD payloads; second call should be served from cache");
    }

    [Fact]
    public void BuildSdmxUrl_constructs_uri_with_iso_period_window()
    {
        var url = EcbFxProvider.BuildSdmxUrl("CHF", AsOf);

        url.Should().StartWith("https://data-api.ecb.europa.eu/service/data/EXR/D.CHF.EUR.SP00.A");
        url.Should().Contain("startPeriod=2026-05-28");
        url.Should().Contain("endPeriod=2026-06-04");
    }

    private static EcbFxProvider BuildProvider(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var factory = new SingleClientHttpFactory(handler);
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 256 });
        var exchangeRates = Substitute.For<IExchangeRateRepository>();
        return new EcbFxProvider(factory, exchangeRates, cache, NullLogger<EcbFxProvider>.Instance);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class SingleClientHttpFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public SingleClientHttpFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
