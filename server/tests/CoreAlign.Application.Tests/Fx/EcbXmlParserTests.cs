using CoreAlign.Infrastructure.Fx.Ecb;

namespace CoreAlign.Application.Tests.Fx;

public class EcbXmlParserTests
{
    private static readonly DateTime DefaultEffective = DateTime.SpecifyKind(new DateTime(2026, 6, 4), DateTimeKind.Utc);

    private const string GenericXml = """
<?xml version="1.0" encoding="UTF-8" ?>
<message:GenericData xmlns:message="http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message" xmlns:generic="http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic">
  <message:DataSet>
    <generic:Series>
      <generic:SeriesKey>
        <generic:Value id="FREQ" value="D" />
        <generic:Value id="CURRENCY" value="USD" />
        <generic:Value id="CURRENCY_DENOM" value="EUR" />
      </generic:SeriesKey>
      <generic:Obs>
        <generic:ObsDimension value="2026-06-04" />
        <generic:ObsValue value="1.0875" />
      </generic:Obs>
    </generic:Series>
    <generic:Series>
      <generic:SeriesKey>
        <generic:Value id="FREQ" value="D" />
        <generic:Value id="CURRENCY" value="TRY" />
        <generic:Value id="CURRENCY_DENOM" value="EUR" />
      </generic:SeriesKey>
      <generic:Obs>
        <generic:ObsDimension value="2026-06-04" />
        <generic:ObsValue value="35.2143" />
      </generic:Obs>
    </generic:Series>
  </message:DataSet>
</message:GenericData>
""";

    [Fact]
    public void Parse_ReturnsEmpty_WhenInputIsBlank()
    {
        var result = EcbXmlParser.Parse(string.Empty, DefaultEffective);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_ReturnsEmpty_WhenInputIsMalformed()
    {
        var result = EcbXmlParser.Parse("<not><well-formed</not>", DefaultEffective);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_ExtractsCurrenciesFromGenericFormat()
    {
        var result = EcbXmlParser.Parse(GenericXml, DefaultEffective);
        Assert.Contains(result, r => r.CurrencyCode == "USD" && r.RateAgainstEur == 1.0875m);
        Assert.Contains(result, r => r.CurrencyCode == "TRY" && r.RateAgainstEur == 35.2143m);
    }

    [Fact]
    public void BuildSdmxUrl_IncludesIso8601StartAndEnd()
    {
        var url = CoreAlign.Infrastructure.Fx.EcbFxProvider.BuildSdmxUrl("USD", DefaultEffective);
        Assert.Contains("EXR/D.USD.EUR.SP00.A", url);
        Assert.Contains("startPeriod=2026-05-28", url);
        Assert.Contains("endPeriod=2026-06-04", url);
    }
}
