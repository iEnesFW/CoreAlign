using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Treasury;

public class TcmbFxClientTests
{
    private const string SampleXml = """
<?xml version="1.0" encoding="ISO-8859-9" ?>
<Tarih_Date Tarih="03.06.2026" Date="06/03/2026" Bulten_No="2026/106">
  <Currency CrossOrder="0" Kod="USD" CurrencyCode="USD">
    <Unit>1</Unit>
    <Isim>ABD DOLARI</Isim>
    <CurrencyName>US DOLLAR</CurrencyName>
    <ForexBuying>32.1234</ForexBuying>
    <ForexSelling>32.1850</ForexSelling>
    <BanknoteBuying>32.1010</BanknoteBuying>
    <BanknoteSelling>32.2335</BanknoteSelling>
  </Currency>
  <Currency CrossOrder="0" Kod="EUR" CurrencyCode="EUR">
    <Unit>1</Unit>
    <Isim>EURO</Isim>
    <CurrencyName>EURO</CurrencyName>
    <ForexBuying>35.6700</ForexBuying>
    <ForexSelling>35.7450</ForexSelling>
    <BanknoteBuying>35.6450</BanknoteBuying>
    <BanknoteSelling>35.7986</BanknoteSelling>
  </Currency>
  <Currency CrossOrder="9" Kod="JPY" CurrencyCode="JPY">
    <Unit>100</Unit>
    <Isim>JAPON YENI</Isim>
    <CurrencyName>JAPENESE YEN</CurrencyName>
    <ForexBuying>20.5512</ForexBuying>
    <ForexSelling>20.6800</ForexSelling>
  </Currency>
</Tarih_Date>
""";

    [Fact]
    public void Parse_returns_one_rate_per_currency_with_units_normalised()
    {
        var rates = TcmbFxClient.Parse(SampleXml);

        rates.Should().HaveCount(3);
        rates.Should().Contain(r => r.Currency == "USD" && r.ForexSelling == 32.185000m);
        rates.Should().Contain(r => r.Currency == "EUR" && r.ForexSelling == 35.745000m);

        var jpy = rates.Single(r => r.Currency == "JPY");
        jpy.ForexSelling.Should().Be(0.206800m, because: "JPY unit is 100; rate must be per single yen");
    }

    [Fact]
    public void Parse_returns_empty_when_xml_is_unparseable()
    {
        var rates = TcmbFxClient.Parse("this is not xml");
        rates.Should().BeEmpty();
    }

    [Fact]
    public void Parse_normalises_currency_codes_to_uppercase()
    {
        const string xml = """
<?xml version="1.0"?>
<Tarih_Date Tarih="03.06.2026">
  <Currency CurrencyCode="gbp">
    <Unit>1</Unit>
    <ForexSelling>41.5320</ForexSelling>
  </Currency>
</Tarih_Date>
""";
        var rates = TcmbFxClient.Parse(xml);
        rates.Should().ContainSingle().Which.Currency.Should().Be("GBP");
    }

    [Fact]
    public void Parse_skips_currencies_missing_selling_rate()
    {
        const string xml = """
<?xml version="1.0"?>
<Tarih_Date Tarih="03.06.2026">
  <Currency CurrencyCode="USD"><Unit>1</Unit><ForexBuying>32</ForexBuying></Currency>
  <Currency CurrencyCode="EUR"><Unit>1</Unit><ForexSelling>35.74</ForexSelling></Currency>
</Tarih_Date>
""";
        var rates = TcmbFxClient.Parse(xml);
        rates.Should().ContainSingle().Which.Currency.Should().Be("EUR");
    }
}
