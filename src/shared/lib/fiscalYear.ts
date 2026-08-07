export const CALENDAR_START_MONTH = 1;

export const normalizeFiscalStartMonth = (startMonth: number | null | undefined): number =>
  Number.isInteger(startMonth) && (startMonth as number) >= 1 && (startMonth as number) <= 12
    ? (startMonth as number)
    : CALENDAR_START_MONTH;

/**
 * Mirrors the backend `FiscalYear` service: a year is labelled by the calendar year it OPENS in,
 * so a period starting October 2026 is "2026" even though most of it falls in 2027. Keep the two
 * in step — the client picks the label, the server derives the date window from it.
 */
export const fiscalYearOf = (instant: Date, startMonth: number | null | undefined): number => {
  const month = normalizeFiscalStartMonth(startMonth);
  return instant.getMonth() + 1 >= month ? instant.getFullYear() : instant.getFullYear() - 1;
};

export const fiscalYearLabel = (year: number, startMonth: number | null | undefined): string =>
  normalizeFiscalStartMonth(startMonth) === CALENDAR_START_MONTH
    ? String(year)
    : `${year}/${year + 1}`;

export const fiscalYearOptions = (currentYear: number, back = 4, forward = 1): number[] => {
  const years: number[] = [];
  for (let y = currentYear + forward; y >= currentYear - back; y -= 1) {
    years.push(y);
  }
  return years;
};
