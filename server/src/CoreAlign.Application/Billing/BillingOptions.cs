namespace CoreAlign.Application.Billing;

/// <summary>
/// Configuration block for the billing module (CoreAlign:Billing). Lets ops
/// pick a default gateway per environment without code changes — e.g. "mock"
/// in dev/staging, "iyzico" in prod.
/// </summary>
public class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>
    /// Gateway used when <c>CreateSubscriptionOrderCommand.GatewayName</c> is null.
    /// Leave empty to force the caller to supply a name (recommended in production).
    /// </summary>
    public string? DefaultGatewayName { get; set; }
}
