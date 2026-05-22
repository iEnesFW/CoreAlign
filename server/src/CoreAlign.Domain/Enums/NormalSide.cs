namespace CoreAlign.Domain.Enums;

/// <summary>
/// The side a balance naturally sits on. Asset and Expense accounts are
/// debit-normal; Liability, Equity, and Revenue are credit-normal. Used by
/// trial-balance / mizan reporting to print positive balances on the correct
/// column.
/// </summary>
public enum NormalSide
{
    Debit = 1,
    Credit = 2,
}
