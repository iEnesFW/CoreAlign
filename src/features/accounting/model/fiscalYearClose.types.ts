import type { JournalEntry } from './journalEntry.types';

export interface YearEndEntry {
  year: number;
  entry: JournalEntry;
  netResult: number;
  alreadyExisted: boolean;
}

export interface FiscalYearCommandBody {
  postedByUserId?: string;
}
