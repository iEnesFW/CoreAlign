import type { AccountType } from '../model/glAccount.types';
import type { TrialBalanceRow } from '../model/journalEntry.types';

/** Signed balance in the account's *normal* direction (always positive when the
 *  account carries its natural balance). Avoids relying on the report's balance
 *  sign convention. */
export const naturalBalance = (r: TrialBalanceRow): number =>
  r.normalSide === 'Debit' ? r.debit - r.credit : r.credit - r.debit;

export interface StatementLine {
  accountId: string;
  accountCode: string;
  accountName: string;
  amount: number;
}

export interface StatementSection {
  lines: StatementLine[];
  total: number;
}

const EPSILON = 0.005;

const sectionFor = (rows: TrialBalanceRow[], types: AccountType[]): StatementSection => {
  const lines = rows
    .filter((r) => types.includes(r.type))
    .map((r) => ({
      accountId: r.accountId,
      accountCode: r.accountCode,
      accountName: r.accountName,
      amount: naturalBalance(r),
    }))
    .filter((l) => Math.abs(l.amount) > EPSILON)
    .sort((a, b) => a.accountCode.localeCompare(b.accountCode));
  const total = lines.reduce((sum, l) => sum + l.amount, 0);
  return { lines, total };
};

export interface IncomeStatement {
  revenue: StatementSection;
  cogs: StatementSection;
  opex: StatementSection;
  grossProfit: number;
  netIncome: number;
}

export const buildIncomeStatement = (rows: TrialBalanceRow[]): IncomeStatement => {
  const revenue = sectionFor(rows, ['Revenue']);
  const cogs = sectionFor(rows, ['CostOfGoodsSold']);
  const opex = sectionFor(rows, ['Expense']);
  const grossProfit = revenue.total - cogs.total;
  const netIncome = grossProfit - opex.total;
  return { revenue, cogs, opex, grossProfit, netIncome };
};

export interface BalanceSheet {
  assets: StatementSection;
  liabilities: StatementSection;
  equity: StatementSection;
  netIncome: number;
  totalLiabilitiesAndEquity: number;
  isBalanced: boolean;
}

export const buildBalanceSheet = (rows: TrialBalanceRow[]): BalanceSheet => {
  const assets = sectionFor(rows, ['Asset']);
  const liabilities = sectionFor(rows, ['Liability']);
  const equity = sectionFor(rows, ['Equity']);
  // Period net income rolls into equity so the sheet balances before closing entries.
  const { netIncome } = buildIncomeStatement(rows);
  const totalLiabilitiesAndEquity = liabilities.total + equity.total + netIncome;
  const isBalanced = Math.abs(assets.total - totalLiabilitiesAndEquity) < 0.01;
  return { assets, liabilities, equity, netIncome, totalLiabilitiesAndEquity, isBalanced };
};
