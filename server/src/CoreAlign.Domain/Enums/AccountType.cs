namespace CoreAlign.Domain.Enums;

/// <summary>
/// High-level account category. Drives normal side (debit/credit), where the
/// balance lands on Bilanço vs Gelir Tablosu, and which Turkish TDHP class the
/// account belongs to (1xx Dönen Varlık, 2xx Duran Varlık, 3xx Kısa Vadeli
/// Borç, 4xx Uzun Vadeli Borç, 5xx Özkaynak, 6xx Gelir, 7xx Maliyet/Gider,
/// 8xx... 9xx Nazım).
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5,
    CostOfGoodsSold = 6,
    Memorandum = 7,
}
