import { CalendarRange } from 'lucide-react';
import { useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';

import { useCompanyProfileQuery } from '@/features/settings/hooks/useSettingsQueries';
import { fiscalYearLabel, fiscalYearOf, fiscalYearOptions } from '@/shared/lib/fiscalYear';
import { useFiscalYearStore } from '@/shared/lib/store/fiscalYearStore';

export const FiscalYearSelect = () => {
  const { t } = useTranslation();
  const settingsQuery = useCompanyProfileQuery();
  const startMonth = useFiscalYearStore((s) => s.startMonth);
  const selectedYear = useFiscalYearStore((s) => s.selectedYear);
  const setStartMonth = useFiscalYearStore((s) => s.setStartMonth);
  const selectYear = useFiscalYearStore((s) => s.selectYear);

  const tenantStartMonth = settingsQuery.data?.data?.fiscalYearStartMonth;

  useEffect(() => {
    setStartMonth(tenantStartMonth);
  }, [tenantStartMonth, setStartMonth]);

  const options = useMemo(
    () => fiscalYearOptions(fiscalYearOf(new Date(), startMonth)),
    [startMonth],
  );

  // The list is a rolling window around today, so a pinned year from an earlier session (or a
  // record older than the window) must still be selectable rather than silently snapping back.
  const years = useMemo(
    () =>
      selectedYear !== null && !options.includes(selectedYear)
        ? [...options, selectedYear].sort((a, b) => b - a)
        : options,
    [options, selectedYear],
  );

  if (selectedYear === null) {
    return null;
  }

  return (
    <label className="mr-1 hidden items-center gap-1 lg:inline-flex">
      <span className="sr-only">{t('FiscalYear.label')}</span>
      <CalendarRange size={14} className="text-slate-400 dark:text-slate-500" aria-hidden="true" />
      <select
        value={selectedYear}
        onChange={(e) => selectYear(Number(e.target.value))}
        title={t('FiscalYear.title')}
        className="rounded-md border border-slate-300 bg-white px-1.5 py-0.5 text-xs font-medium text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
      >
        {years.map((year) => (
          <option key={year} value={year}>
            {fiscalYearLabel(year, startMonth)}
          </option>
        ))}
      </select>
    </label>
  );
};
