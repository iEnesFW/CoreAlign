namespace CoreAlign.Application.Authorization;

/// <summary>
/// Authorization policy names for payment endpoints. <see cref="Charge"/> is
/// any authenticated user (their persona-specific limits are applied inside
/// the dispatcher); <see cref="Refund"/> is restricted to finance / tenant
/// admin personas so cardholders cannot self-refund.
/// </summary>
public static class PaymentPolicies
{
    public const string Charge = "Payment.Charge";
    public const string Refund = "Payment.Refund";

    public const string TenantAdminRole = "TenantAdmin";
    public const string FinanceManagerRole = "FinanceManager";
}
