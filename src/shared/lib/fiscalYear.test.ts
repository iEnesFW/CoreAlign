import { describe, expect, it } from 'vitest';

import {
  fiscalYearLabel,
  fiscalYearOf,
  fiscalYearOptions,
  normalizeFiscalStartMonth,
} from './fiscalYear';

describe('fiscalYear', () => {
  it('labels a year by the calendar year it opens in', () => {
    expect(fiscalYearOf(new Date(2026, 9, 1), 10)).toBe(2026);
    expect(fiscalYearOf(new Date(2027, 8, 30), 10)).toBe(2026);
    expect(fiscalYearOf(new Date(2027, 9, 1), 10)).toBe(2027);
  });

  it('leaves a calendar-year tenant on the plain calendar year', () => {
    expect(fiscalYearOf(new Date(2026, 0, 1), 1)).toBe(2026);
    expect(fiscalYearOf(new Date(2026, 11, 31), 1)).toBe(2026);
  });

  // The backend clamps the same way — tenants.fiscal_year_start_month shipped with a DB default
  // of 0, a month that does not exist.
  it.each([0, 13, -3, null, undefined, 1.5])('falls back to January for %s', (bogus) => {
    expect(normalizeFiscalStartMonth(bogus as number)).toBe(1);
    expect(fiscalYearOf(new Date(2026, 6, 5), bogus as number)).toBe(2026);
  });

  it('spans two calendar years in the label only when the year is offset', () => {
    expect(fiscalYearLabel(2026, 1)).toBe('2026');
    expect(fiscalYearLabel(2026, 10)).toBe('2026/2027');
  });

  it('offers a rolling window newest first', () => {
    expect(fiscalYearOptions(2026, 2, 1)).toEqual([2027, 2026, 2025, 2024]);
  });
});
