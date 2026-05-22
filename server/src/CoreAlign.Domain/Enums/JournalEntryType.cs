namespace CoreAlign.Domain.Enums;

/// <summary>
/// Turkish journal-entry classification — drives the document sequence prefix
/// and routes the entry to the right report column. The classification is
/// loose and informational; the only hard rule is <see cref="Mahsup"/> for any
/// entry that mixes cash and non-cash sides.
/// </summary>
public enum JournalEntryType
{
    /// <summary>Tahsil fişi — incoming cash/bank receipt.</summary>
    Tahsil = 1,
    /// <summary>Tediye fişi — outgoing cash/bank payment.</summary>
    Tediye = 2,
    /// <summary>Mahsup fişi — non-cash journal (purchase, sale, accrual, etc.).</summary>
    Mahsup = 3,
    /// <summary>Açılış fişi — opening balance entry at fiscal year start.</summary>
    Acilis = 4,
    /// <summary>Kapanış fişi — closing entry (revenue/expense → 690 → 591/592).</summary>
    Kapanis = 5,
}
