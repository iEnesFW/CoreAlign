using CoreAlign.Infrastructure.Fx.Tcmb;

namespace CoreAlign.Application.Tests.Fx;

public class TcmbXmlParserTests
{
    private static readonly DateTime DefaultEffective = DateTime.SpecifyKind(new DateTime(2026, 6, 4), DateTimeKind.Utc);

    private const string RealisticTcmbXml = """
<?xml version="1.0" encoding="ISO-8859-9" ?>
<Tarih_Date Tarih="04.06.2026" Date="06/04/2026" Bulten_No="2026/108">
  <Currency CrossOrder="0" Kod="USD" CurrencyCode="USD">
    <Unit>1</Unit>
    <Isim>ABD DOLARI</Isim>
    <CurrencyName>US DOLLAR</CurrencyName>
    <ForexBuying>32.4521</ForexBuying>
    <ForexSelling>32.5123</ForexSelling>
    <BanknoteBuying>32.4296</BanknoteBuying>
    <BanknoteSelling>32.5611</BanknoteSelling>
  </Currency>
  <Currency CrossOrder="1" Kod="AUD" CurrencyCode="AUD">
    <Unit>1</Unit>
    <Isim>AVUSTRALYA DOLARI</Isim>
    <CurrencyName>AUSTRALIAN DOLLAR</CurrencyName>
    <ForexBuying>21.3120</ForexBuying>
    <ForexSelling>21.4427</ForexSelling>
    <BanknoteBuying>21.1419</BanknoteBuying>
    <BanknoteSelling>21.5765</BanknoteSelling>
  </Currency>
  <Currency CrossOrder="2" Kod="DKK" CurrencyCode="DKK">
    <Unit>1</Unit>
    <Isim>DANIMARKA KRONU</Isim>
    <CurrencyName>DANISH KRONE</CurrencyName>
    <ForexBuying>4.7820</ForexBuying>
    <ForexSelling>4.8154</ForexSelling>
  </Currency>
  <Currency CrossOrder="3" Kod="EUR" CurrencyCode="EUR">
    <Unit>1</Unit>
    <Isim>EURO</Isim>
    <CurrencyName>EURO</CurrencyName>
    <ForexBuying>35.6789</ForexBuying>
    <ForexSelling>35.7432</ForexSelling>
  </Currency>
  <Currency CrossOrder="4" Kod="GBP" CurrencyCode="GBP">
    <Unit>1</Unit>
    <Isim>INGILIZ STERLINI</Isim>
    <CurrencyName>POUND STERLING</CurrencyName>
    <ForexBuying>41.6123</ForexBuying>
    <ForexSelling>41.8201</ForexSelling>
  </Currency>
  <Currency CrossOrder="5" Kod="CHF" CurrencyCode="CHF">
    <Unit>1</Unit>
    <Isim>ISVICRE FRANGI</Isim>
    <CurrencyName>SWISS FRANK</CurrencyName>
    <ForexBuying>36.1230</ForexBuying>
    <ForexSelling>36.2150</ForexSelling>
  </Currency>
  <Currency CrossOrder="6" Kod="SEK" CurrencyCode="SEK">
    <Unit>1</Unit>
    <Isim>ISVEC KRONU</Isim>
    <CurrencyName>SWEDISH KRONA</CurrencyName>
    <ForexBuying>3.0512</ForexBuying>
    <ForexSelling>3.0710</ForexSelling>
  </Currency>
  <Currency CrossOrder="7" Kod="CAD" CurrencyCode="CAD">
    <Unit>1</Unit>
    <Isim>KANADA DOLARI</Isim>
    <CurrencyName>CANADIAN DOLLAR</CurrencyName>
    <ForexBuying>23.6710</ForexBuying>
    <ForexSelling>23.8001</ForexSelling>
  </Currency>
  <Currency CrossOrder="8" Kod="KWD" CurrencyCode="KWD">
    <Unit>1</Unit>
    <Isim>KUVEYT DINARI</Isim>
    <CurrencyName>KUWAITI DINAR</CurrencyName>
    <ForexBuying>105.4350</ForexBuying>
    <ForexSelling>106.1212</ForexSelling>
  </Currency>
  <Currency CrossOrder="9" Kod="NOK" CurrencyCode="NOK">
    <Unit>1</Unit>
    <Isim>NORVEC KRONU</Isim>
    <CurrencyName>NORWEGIAN KRONE</CurrencyName>
    <ForexBuying>3.0223</ForexBuying>
    <ForexSelling>3.0420</ForexSelling>
  </Currency>
  <Currency CrossOrder="10" Kod="SAR" CurrencyCode="SAR">
    <Unit>1</Unit>
    <Isim>SUUDI ARABISTAN RIYALI</Isim>
    <CurrencyName>SAUDI RIYAL</CurrencyName>
    <ForexBuying>8.6520</ForexBuying>
    <ForexSelling>8.6701</ForexSelling>
  </Currency>
  <Currency CrossOrder="11" Kod="JPY" CurrencyCode="JPY">
    <Unit>100</Unit>
    <Isim>JAPON YENI</Isim>
    <CurrencyName>JAPENESE YEN</CurrencyName>
    <ForexBuying>20.5512</ForexBuying>
    <ForexSelling>20.6800</ForexSelling>
  </Currency>
  <Currency CrossOrder="12" Kod="BGN" CurrencyCode="BGN">
    <Unit>1</Unit>
    <Isim>BULGAR LEVASI</Isim>
    <CurrencyName>BULGARIAN LEV</CurrencyName>
    <ForexBuying>18.2410</ForexBuying>
    <ForexSelling>18.3120</ForexSelling>
  </Currency>
  <Currency CrossOrder="13" Kod="RON" CurrencyCode="RON">
    <Unit>1</Unit>
    <Isim>RUMEN LEYI</Isim>
    <CurrencyName>NEW LEU</CurrencyName>
    <ForexBuying>7.1620</ForexBuying>
    <ForexSelling>7.1932</ForexSelling>
  </Currency>
  <Currency CrossOrder="14" Kod="RUB" CurrencyCode="RUB">
    <Unit>1</Unit>
    <Isim>RUS RUBLESI</Isim>
    <CurrencyName>RUSSIAN ROUBLE</CurrencyName>
    <ForexBuying>0.4022</ForexBuying>
    <ForexSelling>0.4048</ForexSelling>
  </Currency>
  <Currency CrossOrder="15" Kod="IRR" CurrencyCode="IRR">
    <Unit>100</Unit>
    <Isim>IRAN RIYALI</Isim>
    <CurrencyName>IRANIAN RIAL</CurrencyName>
    <ForexBuying>0.0763</ForexBuying>
    <ForexSelling>0.0768</ForexSelling>
  </Currency>
  <Currency CrossOrder="16" Kod="CNY" CurrencyCode="CNY">
    <Unit>1</Unit>
    <Isim>CIN YUANI</Isim>
    <CurrencyName>CHINESE RENMINBI</CurrencyName>
    <ForexBuying>4.4920</ForexBuying>
    <ForexSelling>4.5234</ForexSelling>
  </Currency>
  <Currency CrossOrder="17" Kod="PKR" CurrencyCode="PKR">
    <Unit>1</Unit>
    <Isim>PAKISTAN RUPISI</Isim>
    <CurrencyName>PAKISTANI RUPEE</CurrencyName>
    <ForexBuying>0.1162</ForexBuying>
    <ForexSelling>0.1175</ForexSelling>
  </Currency>
  <Currency CrossOrder="18" Kod="QAR" CurrencyCode="QAR">
    <Unit>1</Unit>
    <Isim>KATAR RIYALI</Isim>
    <CurrencyName>QATARI RIAL</CurrencyName>
    <ForexBuying>8.9123</ForexBuying>
    <ForexSelling>8.9510</ForexSelling>
  </Currency>
  <Currency CrossOrder="19" Kod="KRW" CurrencyCode="KRW">
    <Unit>1000</Unit>
    <Isim>GUNEY KORE WONU</Isim>
    <CurrencyName>SOUTH KOREAN WON</CurrencyName>
    <ForexBuying>23.4120</ForexBuying>
    <ForexSelling>23.5410</ForexSelling>
  </Currency>
  <Currency CrossOrder="20" Kod="AZN" CurrencyCode="AZN">
    <Unit>1</Unit>
    <Isim>AZERBAYCAN YENI MANATI</Isim>
    <CurrencyName>AZERBAIJANI NEW MANAT</CurrencyName>
    <ForexBuying>19.0810</ForexBuying>
    <ForexSelling>19.1923</ForexSelling>
  </Currency>
</Tarih_Date>
""";

    [Fact]
    public void Parse_realistic_payload_returns_one_entry_per_currency()
    {
        var rates = TcmbXmlParser.Parse(RealisticTcmbXml, DefaultEffective);

        rates.Should().HaveCount(21, "21 Currency nodes were present in the bulletin");
        rates.Select(r => r.CurrencyCode).Should().OnlyHaveUniqueItems();
        rates.Should().AllSatisfy(r =>
        {
            r.ForexBuying.Should().BeGreaterThan(0m);
            r.ForexSelling.Should().BeGreaterThan(0m);
        });
    }

    [Fact]
    public void Parse_normalises_unit_attribute_so_jpy_per_100_collapses_to_per_unit()
    {
        var rates = TcmbXmlParser.Parse(RealisticTcmbXml, DefaultEffective);

        var jpy = rates.Should().ContainSingle(r => r.CurrencyCode == "JPY").Subject;
        jpy.Unit.Should().Be(100);
        jpy.NormalizedBuying.Should().Be(0.205512m, "TCMB publishes JPY per 100 — provider must store per-single-yen");
        jpy.NormalizedSelling.Should().Be(0.2068m);

        var krw = rates.Should().ContainSingle(r => r.CurrencyCode == "KRW").Subject;
        krw.Unit.Should().Be(1000);
        krw.NormalizedSelling.Should().Be(0.023541m);

        var usd = rates.Should().ContainSingle(r => r.CurrencyCode == "USD").Subject;
        usd.Unit.Should().Be(1);
        usd.NormalizedBuying.Should().Be(32.4521m);
    }

    [Fact]
    public void Parse_uses_tarih_attribute_when_present_for_effective_date()
    {
        var rates = TcmbXmlParser.Parse(RealisticTcmbXml, DateTime.SpecifyKind(new DateTime(2099, 1, 1), DateTimeKind.Utc));

        rates.Should().NotBeEmpty();
        rates.First().EffectiveDate.Should().Be(DateTime.SpecifyKind(new DateTime(2026, 6, 4), DateTimeKind.Utc));
    }

    [Fact]
    public void Parse_returns_empty_for_malformed_xml()
    {
        var rates = TcmbXmlParser.Parse("<<not-real-xml>>", DefaultEffective);

        rates.Should().BeEmpty();
    }

    [Fact]
    public void Parse_returns_empty_for_empty_payload()
    {
        TcmbXmlParser.Parse(string.Empty, DefaultEffective).Should().BeEmpty();
        TcmbXmlParser.Parse("   ", DefaultEffective).Should().BeEmpty();
    }

    [Fact]
    public void Parse_filters_currencies_missing_forex_buying_or_selling()
    {
        const string xml = """
<?xml version="1.0"?>
<Tarih_Date Tarih="04.06.2026">
  <Currency CurrencyCode="USD"><Unit>1</Unit><ForexBuying>32.4521</ForexBuying><ForexSelling>32.5123</ForexSelling></Currency>
  <Currency CurrencyCode="EUR"><Unit>1</Unit><ForexBuying>35.6789</ForexBuying></Currency>
  <Currency CurrencyCode="GBP"><Unit>1</Unit><ForexSelling>41.8201</ForexSelling></Currency>
  <Currency CurrencyCode="CHF"><Unit>1</Unit><ForexBuying>0</ForexBuying><ForexSelling>0</ForexSelling></Currency>
</Tarih_Date>
""";

        var rates = TcmbXmlParser.Parse(xml, DefaultEffective);

        rates.Should().ContainSingle();
        rates[0].CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Parse_uppercases_currency_codes()
    {
        const string xml = """
<?xml version="1.0"?>
<Tarih_Date Tarih="04.06.2026">
  <Currency CurrencyCode="usd"><Unit>1</Unit><ForexBuying>32.4521</ForexBuying><ForexSelling>32.5123</ForexSelling></Currency>
</Tarih_Date>
""";

        var rates = TcmbXmlParser.Parse(xml, DefaultEffective);

        rates.Should().ContainSingle().Which.CurrencyCode.Should().Be("USD");
    }
}
