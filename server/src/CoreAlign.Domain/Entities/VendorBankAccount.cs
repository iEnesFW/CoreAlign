using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// A vendor's bank account used for outgoing payments. Stored encrypted-at-rest
/// at the column level via EF value conversion when (and if) the team enables
/// data protection; for now stored plaintext within the tenant scope.
/// </summary>
public class VendorBankAccount : TenantEntity
{
    public Guid VendorId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string? Swift { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? AccountNumber { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }

    public Vendor Vendor { get; set; } = null!;

    protected VendorBankAccount() { }

    public VendorBankAccount(Guid vendorId, string bankName, string accountHolder, string iban, string currency = "TRY")
    {
        VendorId = vendorId;
        BankName = bankName;
        AccountHolder = accountHolder;
        Iban = iban.Replace(" ", string.Empty).ToUpperInvariant();
        Currency = currency.Trim().ToUpperInvariant();
    }

    public void Update(
        string bankName,
        string? branchName,
        string accountHolder,
        string iban,
        string? swift,
        string currency,
        string? accountNumber,
        bool isPrimary,
        string? notes)
    {
        BankName = bankName;
        BranchName = branchName;
        AccountHolder = accountHolder;
        Iban = iban.Replace(" ", string.Empty).ToUpperInvariant();
        Swift = swift?.Trim().ToUpperInvariant();
        Currency = currency.Trim().ToUpperInvariant();
        AccountNumber = accountNumber;
        IsPrimary = isPrimary;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
