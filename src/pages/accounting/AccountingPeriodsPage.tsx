import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Calendar, Lock, LockOpen, Plus, RefreshCcw, ShieldAlert } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useAccountingPeriods,
  useClosePeriod,
  useCreateAccountingPeriod,
  useLockPeriod,
  useReopenPeriod,
} from '@/features/pricing/hooks/usePricingQueries';
import type {
  AccountingPeriod,
  AccountingPeriodStatus,
} from '@/features/pricing/model/pricing.types';

const STATUS_STYLES: Record<AccountingPeriodStatus, string> = {
  Open: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Closing: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Closed: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Locked: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const fmtDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

export const AccountingPeriodsPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());
  const periodsQuery = useAccountingPeriods(selectedYear);
  const createMutation = useCreateAccountingPeriod();
  const closeMutation = useClosePeriod();
  const reopenMutation = useReopenPeriod();
  const lockMutation = useLockPeriod();

  const periods = periodsQuery.data?.data ?? [];

  const allMonths = Array.from({ length: 12 }, (_, i) => i + 1);
  const monthLabel = (m: number) =>
    new Date(2024, m - 1, 1).toLocaleString(locale, { month: 'long' });

  const periodFor = (month: number) => periods.find((p) => p.month === month);

  const createForMonth = async (month: number) => {
    try {
      await createMutation.mutateAsync({ year: selectedYear, month });
      toast.success(t('accounting.periods.created', { defaultValue: 'Period created' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const closePeriod = async (period: AccountingPeriod) => {
    if (
      !confirm(
        t('accounting.periods.confirmClose', {
          defaultValue: 'Close period {{code}}?',
          code: period.code,
        }),
      )
    )
      return;
    try {
      await closeMutation.mutateAsync({ id: period.id });
      toast.success(t('accounting.periods.closed', { defaultValue: 'Period closed' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const reopenPeriod = async (period: AccountingPeriod) => {
    try {
      await reopenMutation.mutateAsync(period.id);
      toast.success(t('accounting.periods.reopened', { defaultValue: 'Period reopened' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const lockPeriod = async (period: AccountingPeriod) => {
    if (
      !confirm(
        t('accounting.periods.confirmLock', {
          defaultValue: 'Lock period {{code}}? This cannot be undone.',
          code: period.code,
        }),
      )
    )
      return;
    try {
      await lockMutation.mutateAsync(period.id);
      toast.success(t('accounting.periods.locked', { defaultValue: 'Period locked' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('accounting.periods.title', { defaultValue: 'Accounting periods' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('accounting.periods.subtitle', {
              defaultValue: 'Close periods to prevent posting; lock to make it permanent.',
            })}
          </p>
        </div>
        <div className="inline-flex items-center gap-2">
          <button
            type="button"
            onClick={() => setSelectedYear((y) => y - 1)}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
          >
            ←
          </button>
          <span className="font-semibold text-slate-900 dark:text-slate-100">{selectedYear}</span>
          <button
            type="button"
            onClick={() => setSelectedYear((y) => y + 1)}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
          >
            →
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {allMonths.map((month) => {
          const period = periodFor(month);
          return (
            <div
              key={month}
              className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900"
            >
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-1.5 text-sm font-semibold text-slate-900 dark:text-slate-100">
                    <Calendar size={13} />
                    {monthLabel(month)} {selectedYear}
                  </div>
                  {period && (
                    <div className="mt-0.5 font-mono text-[10px] text-slate-500">{period.code}</div>
                  )}
                </div>
                {period && (
                  <span
                    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_STYLES[period.status]}`}
                  >
                    {t(`accounting.status.${period.status}`, { defaultValue: period.status })}
                  </span>
                )}
              </div>

              {period && period.closedAtUtc && (
                <div className="mt-2 text-[10px] text-slate-500 dark:text-slate-400">
                  {t('accounting.periods.closedAt', { defaultValue: 'Closed' })}:{' '}
                  {fmtDate(period.closedAtUtc, locale)}
                </div>
              )}
              {period?.notes && (
                <div className="mt-1 line-clamp-2 text-[11px] italic text-slate-600 dark:text-slate-400">
                  {period.notes}
                </div>
              )}

              <div className="mt-3 flex flex-wrap gap-1">
                {!period && (
                  <button
                    type="button"
                    onClick={() => createForMonth(month)}
                    className="inline-flex items-center gap-1 rounded border border-indigo-200 bg-white px-2 py-1 text-[11px] font-medium text-indigo-700 hover:bg-indigo-50 dark:border-indigo-500/30 dark:bg-slate-900 dark:text-indigo-300"
                  >
                    <Plus size={11} />
                    {t('accounting.periods.create', { defaultValue: 'Open period' })}
                  </button>
                )}
                {period?.status === 'Open' && (
                  <>
                    <button
                      type="button"
                      onClick={() => closePeriod(period)}
                      className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
                    >
                      <Lock size={11} />
                      {t('accounting.periods.close', { defaultValue: 'Close' })}
                    </button>
                  </>
                )}
                {period?.status === 'Closed' && (
                  <>
                    <button
                      type="button"
                      onClick={() => reopenPeriod(period)}
                      className="inline-flex items-center gap-1 rounded border border-amber-200 bg-white px-2 py-1 text-[11px] font-medium text-amber-700 hover:bg-amber-50 dark:border-amber-500/30 dark:bg-slate-900 dark:text-amber-300"
                    >
                      <RefreshCcw size={11} />
                      {t('accounting.periods.reopen', { defaultValue: 'Reopen' })}
                    </button>
                    <button
                      type="button"
                      onClick={() => lockPeriod(period)}
                      className="inline-flex items-center gap-1 rounded border border-rose-200 bg-white px-2 py-1 text-[11px] font-medium text-rose-700 hover:bg-rose-50 dark:border-rose-500/30 dark:bg-slate-900 dark:text-rose-300"
                    >
                      <ShieldAlert size={11} />
                      {t('accounting.periods.lock', { defaultValue: 'Lock' })}
                    </button>
                  </>
                )}
                {period?.status === 'Locked' && (
                  <span className="inline-flex items-center gap-1 text-[11px] text-slate-500 dark:text-slate-400">
                    <LockOpen size={11} />
                    {t('accounting.periods.lockedHint', {
                      defaultValue: 'Permanent — cannot be reopened',
                    })}
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default AccountingPeriodsPage;
