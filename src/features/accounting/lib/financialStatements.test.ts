import { describe, expect, it } from 'vitest';
import type { TrialBalanceRow } from '../model/journalEntry.types';
import { buildBalanceSheet, buildIncomeStatement, naturalBalance } from './financialStatements';

const row = (over: Partial<TrialBalanceRow>): TrialBalanceRow => ({
  accountId: over.accountCode ?? 'x',
  accountCode: '000',
  accountName: 'Acct',
  type: 'Asset',
  normalSide: 'Debit',
  debit: 0,
  credit: 0,
  balance: 0,
  ...over,
});

// A small but balanced ledger: Assets 1000 = Liabilities 300 + Equity 400 + NetIncome 300.
const rows: TrialBalanceRow[] = [
  row({ accountCode: '100', type: 'Asset', normalSide: 'Debit', debit: 1000, credit: 0 }),
  row({ accountCode: '300', type: 'Liability', normalSide: 'Credit', debit: 0, credit: 300 }),
  row({ accountCode: '500', type: 'Equity', normalSide: 'Credit', debit: 0, credit: 400 }),
  row({ accountCode: '600', type: 'Revenue', normalSide: 'Credit', debit: 0, credit: 900 }),
  row({ accountCode: '700', type: 'CostOfGoodsSold', normalSide: 'Debit', debit: 400, credit: 0 }),
  row({ accountCode: '800', type: 'Expense', normalSide: 'Debit', debit: 200, credit: 0 }),
];

describe('naturalBalance', () => {
  it('returns positive for an account carrying its normal balance', () => {
    expect(naturalBalance(row({ normalSide: 'Debit', debit: 100, credit: 30 }))).toBe(70);
    expect(naturalBalance(row({ normalSide: 'Credit', debit: 30, credit: 100 }))).toBe(70);
  });
});

describe('buildIncomeStatement', () => {
  it('computes gross profit and net income', () => {
    const s = buildIncomeStatement(rows);
    expect(s.revenue.total).toBe(900);
    expect(s.cogs.total).toBe(400);
    expect(s.opex.total).toBe(200);
    expect(s.grossProfit).toBe(500);
    expect(s.netIncome).toBe(300);
  });

  it('excludes zero-balance accounts', () => {
    const s = buildIncomeStatement([
      row({ accountCode: '600', type: 'Revenue', normalSide: 'Credit', debit: 50, credit: 50 }),
    ]);
    expect(s.revenue.lines).toHaveLength(0);
  });
});

describe('buildBalanceSheet', () => {
  it('balances assets against liabilities + equity + net income', () => {
    const b = buildBalanceSheet(rows);
    expect(b.assets.total).toBe(1000);
    expect(b.liabilities.total).toBe(300);
    expect(b.equity.total).toBe(400);
    expect(b.netIncome).toBe(300);
    expect(b.totalLiabilitiesAndEquity).toBe(1000);
    expect(b.isBalanced).toBe(true);
  });

  it('flags an unbalanced ledger', () => {
    const b = buildBalanceSheet([
      row({ accountCode: '100', type: 'Asset', normalSide: 'Debit', debit: 1000, credit: 0 }),
    ]);
    expect(b.isBalanced).toBe(false);
  });
});
