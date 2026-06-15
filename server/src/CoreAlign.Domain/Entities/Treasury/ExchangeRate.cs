using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Treasury;

public class ExchangeRate : TenantEntity, IGlobalReadable
{
    public string Currency { get; set; } = string.Empty;
    public decimal RateAgainstTry { get; set; }
    public DateTime ValidOnDate { get; set; }
    public string Source { get; set; } = "TCMB";
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
}
