import { create } from 'zustand';

import { fiscalYearOf, normalizeFiscalStartMonth } from '../fiscalYear';

interface FiscalYearState {
  startMonth: number;
  selectedYear: number | null;
  pinned: boolean;
  setStartMonth: (startMonth: number | null | undefined) => void;
  selectYear: (year: number) => void;
  clearSelection: () => void;
}

/**
 * Cross-cutting: the working year is read by every list screen and written by one selector in the
 * navbar, so it lives in shared rather than in a feature (the persona/auth store precedent).
 *
 * `pinned` separates "the user chose a year" from "we defaulted to today's year". Only a pinned
 * year survives a change to the tenant's fiscal start month — otherwise moving the start month
 * from January to October would strand the user in a year that no longer means what it did.
 */
export const useFiscalYearStore = create<FiscalYearState>((set, get) => ({
  startMonth: 1,
  selectedYear: null,
  pinned: false,
  setStartMonth: (startMonth) => {
    const month = normalizeFiscalStartMonth(startMonth);
    if (get().startMonth === month && get().selectedYear !== null) {
      return;
    }
    const current = fiscalYearOf(new Date(), month);
    set((s) => ({
      startMonth: month,
      selectedYear: s.pinned ? s.selectedYear : current,
    }));
  },
  selectYear: (year) => set({ selectedYear: year, pinned: true }),
  clearSelection: () =>
    set((s) => ({ selectedYear: fiscalYearOf(new Date(), s.startMonth), pinned: false })),
}));

export const useActiveFiscalYear = (): number | null => useFiscalYearStore((s) => s.selectedYear);
