import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Calendar,
  ChevronLeft,
  ChevronRight,
  Lock,
  LockOpen,
  Plus,
  RefreshCcw,
  ShieldAlert,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
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

const STATUS_VARIANTS: Record<AccountingPeriodStatus, BadgeVariant> = {
  Open: 'success',
  Closing: 'warning',
  Closed: 'neutral',
  Locked: 'danger',
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
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Calendar size={20} />}
          title={t('accounting.periods.title', { defaultValue: 'Accounting periods' })}
          subtitle={t('accounting.periods.subtitle', {
            defaultValue: 'Close periods to prevent posting; lock to make it permanent.',
          })}
          trailing={
            <div className="inline-flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setSelectedYear((y) => y - 1)}
              >
                <ChevronLeft size={14} />
              </Button>
              <span className="font-semibold text-slate-900 dark:text-slate-100">
                {selectedYear}
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setSelectedYear((y) => y + 1)}
              >
                <ChevronRight size={14} />
              </Button>
            </div>
          }
        />
      }
    >
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
                  <Badge variant={STATUS_VARIANTS[period.status]}>
                    {t(`accounting.status.${period.status}`, { defaultValue: period.status })}
                  </Badge>
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
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => createForMonth(month)}
                  >
                    <Plus size={11} />
                    {t('accounting.periods.create', { defaultValue: 'Open period' })}
                  </Button>
                )}
                {period?.status === 'Open' && (
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => closePeriod(period)}
                  >
                    <Lock size={11} />
                    {t('accounting.periods.close', { defaultValue: 'Close' })}
                  </Button>
                )}
                {period?.status === 'Closed' && (
                  <>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => reopenPeriod(period)}
                    >
                      <RefreshCcw size={11} />
                      {t('accounting.periods.reopen', { defaultValue: 'Reopen' })}
                    </Button>
                    <Button
                      type="button"
                      variant="danger"
                      size="sm"
                      onClick={() => lockPeriod(period)}
                    >
                      <ShieldAlert size={11} />
                      {t('accounting.periods.lock', { defaultValue: 'Lock' })}
                    </Button>
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
    </ListPageTemplate>
  );
};

export default AccountingPeriodsPage;
