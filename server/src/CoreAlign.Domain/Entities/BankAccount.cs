using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class BankAccount : TenantEntity
{
    public string AccountName { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public string? BranchName { get; private set; }
    public string Iban { get; private set; } = string.Empty;
    public string? Swift { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal OpeningBalance { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    protected BankAccount() { }

    public BankAccount(
        string accountName,
        string bankName,
        string iban,
        string currency = "TRY",
        decimal openingBalance = 0m,
        string? branchName = null,
        string? swift = null,
        bool isPrimary = false,
        string? notes = null)
    {
        AccountName = accountName;
        BankName = bankName;
        Iban = NormalizeIban(iban);
        Currency = NormalizeCurrency(currency);
        OpeningBalance = openingBalance;
        BranchName = branchName;
        Swift = swift;
        IsPrimary = isPrimary;
        Notes = notes;
    }

    public void Update(
        string accountName,
        string bankName,
        string iban,
        string currency,
        decimal openingBalance,
        string? branchName,
        string? swift,
        bool isPrimary,
        bool isActive,
        string? notes)
    {
        AccountName = accountName;
        BankName = bankName;
        Iban = NormalizeIban(iban);
        Currency = NormalizeCurrency(currency);
        OpeningBalance = openingBalance;
        BranchName = branchName;
        Swift = swift;
        IsPrimary = isPrimary;
        IsActive = isActive;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearPrimary()
    {
        if (!IsPrimary) return;
        IsPrimary = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeIban(string iban) =>
        (iban ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

    private static string NormalizeCurrency(string currency) =>
        (currency ?? "TRY").Trim().ToUpperInvariant();
}
