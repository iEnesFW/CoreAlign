import type { AccountType, NormalSide } from './glAccount.types';

export type JournalEntryType = 'Tahsil' | 'Tediye' | 'Mahsup' | 'Acilis' | 'Kapanis';
export type JournalEntryStatus = 'Draft' | 'Posted' | 'Reversed';

export interface JournalLine {
  id: string;
  lineNumber: number;
  accountId: string;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  currency: string;
  description?: string | null;
  costCenter?: string | null;
  project?: string | null;
  foreignAmount?: number | null;
  exchangeRate?: number | null;
}

export interface JournalEntry {
  id: string;
  number: string;
  entryDate: string;
  postingDate: string;
  type: JournalEntryType;
  status: JournalEntryStatus;
  description?: string | null;
  reference?: string | null;
  totalDebit: number;
  totalCredit: number;
  postedAtUtc?: string | null;
  reversedAtUtc?: string | null;
  reversalOfId?: string | null;
  reversedById?: string | null;
  lines: JournalLine[];
}

export interface JournalEntrySummary {
  id: string;
  number: string;
  entryDate: string;
  postingDate: string;
  type: JournalEntryType;
  status: JournalEntryStatus;
  description?: string | null;
  reference?: string | null;
  totalDebit: number;
  totalCredit: number;
  lineCount: number;
}

export interface JournalLineInput {
  accountId: string;
  debit: number;
  credit: number;
  currency?: string;
  description?: string | null;
  costCenter?: string | null;
  project?: string | null;
  foreignAmount?: number | null;
  exchangeRate?: number | null;
}

export interface CreateJournalEntryRequest {
  entryDate: string;
  postingDate: string;
  type: JournalEntryType;
  description?: string | null;
  reference?: string | null;
  lines: JournalLineInput[];
  postImmediately?: boolean;
}

export interface JournalEntryListParams {
  search?: string;
  type?: JournalEntryType;
  status?: JournalEntryStatus;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface TrialBalanceRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  type: AccountType;
  normalSide: NormalSide;
  debit: number;
  credit: number;
  balance: number;
}

export interface TrialBalanceReport {
  fromDate?: string | null;
  toDate?: string | null;
  totalDebit: number;
  totalCredit: number;
  rows: TrialBalanceRow[];
}
