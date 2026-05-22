using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// General-ledger account in the chart of accounts (Hesap Planı). Models the
/// Turkish "Tek Düzen Hesap Planı" hierarchy where codes nest by prefix: 1
/// (Dönen Varlık) → 10 → 100 → 100.01. Posting is only allowed at the leaf
/// level (<see cref="IsPostable"/>), enforced at journal-entry validation.
/// </summary>
public class GLAccount : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AccountType Type { get; private set; }
    public NormalSide NormalSide { get; private set; }
    public Guid? ParentId { get; private set; }
    public GLAccount? Parent { get; private set; }
    public int Level { get; private set; }
    public bool IsPostable { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string Currency { get; private set; } = "TRY";

    protected GLAccount() { }

    public GLAccount(
        string code,
        string name,
        AccountType type,
        bool isPostable,
        Guid? parentId = null,
        int level = 1,
        string currency = "TRY",
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Account code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name is required.", nameof(name));
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), "Level must be >= 1.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        Code = code.Trim();
        Name = name.Trim();
        Description = description?.Trim();
        Type = type;
        NormalSide = DeriveNormalSide(type);
        ParentId = parentId;
        Level = level;
        IsPostable = isPostable;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public void Rename(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangePostable(bool postable)
    {
        // A non-postable account becoming postable is fine. The reverse is
        // safe as long as nothing has already posted to it — the application
        // layer enforces that precondition via the repository.
        if (IsPostable == postable) return;
        IsPostable = postable;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        var normalized = currency.Trim().ToUpperInvariant();
        if (Currency == normalized) return;
        Currency = normalized;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Internal-use helper for bulk-seed scenarios where the parent has not yet
    /// been persisted at construction time. Application-layer code should not
    /// call this on already-persisted accounts.
    /// </summary>
    public void AssignParent(Guid parentId, int level)
    {
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), "Level must be >= 1.");
        ParentId = parentId;
        Level = level;
    }

    private static NormalSide DeriveNormalSide(AccountType type) => type switch
    {
        AccountType.Asset => NormalSide.Debit,
        AccountType.Expense => NormalSide.Debit,
        AccountType.CostOfGoodsSold => NormalSide.Debit,
        AccountType.Liability => NormalSide.Credit,
        AccountType.Equity => NormalSide.Credit,
        AccountType.Revenue => NormalSide.Credit,
        AccountType.Memorandum => NormalSide.Debit,
        _ => NormalSide.Debit,
    };
}
