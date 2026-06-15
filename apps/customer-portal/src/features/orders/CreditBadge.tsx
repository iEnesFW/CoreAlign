import { useTranslation } from 'react-i18next';
import { AlertTriangle, ShieldAlert, ShieldCheck } from 'lucide-react';
import { useCreditSnapshot } from '@/features/portal/hooks';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { Spinner } from '@/shared/ui/Spinner';

export const CreditBadge = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const credit = useCreditSnapshot();

  if (credit.isLoading) {
    return (
      <div className="flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        <Spinner size={14} />
      </div>
    );
  }

  const data = credit.data;
  if (!data || data.limit <= 0) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        {t('credit.noLimit')}
      </div>
    );
  }

  const tone = data.isHardLimitReached
    ? 'text-rose-700 bg-rose-50 border-rose-200 dark:text-rose-200 dark:bg-rose-900/40 dark:border-rose-700'
    : data.isSoftLimitReached
      ? 'text-amber-800 bg-amber-50 border-amber-200 dark:text-amber-200 dark:bg-amber-900/30 dark:border-amber-700'
      : 'text-emerald-700 bg-emerald-50 border-emerald-200 dark:text-emerald-200 dark:bg-emerald-900/30 dark:border-emerald-700';

  const Icon = data.isHardLimitReached
    ? ShieldAlert
    : data.isSoftLimitReached
      ? AlertTriangle
      : ShieldCheck;

  return (
    <div className={`flex flex-col gap-1 rounded-xl border px-4 py-3 text-xs ${tone}`}>
      <div className="flex items-center gap-2 text-sm font-semibold">
        <Icon size={16} />
        <span>{t('credit.title')}</span>
      </div>
      <div className="grid grid-cols-3 gap-2">
        <div>
          <p className="opacity-70">{t('credit.limit')}</p>
          <p className="font-medium">{formatCurrency(data.limit, locale, data.currency)}</p>
        </div>
        <div>
          <p className="opacity-70">{t('credit.outstanding')}</p>
          <p className="font-medium">{formatCurrency(data.outstanding, locale, data.currency)}</p>
        </div>
        <div>
          <p className="opacity-70">{t('credit.available')}</p>
          <p className="font-medium">{formatCurrency(data.available, locale, data.currency)}</p>
        </div>
      </div>
      <div className="mt-1 flex items-center gap-2">
        <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
          <div
            className={`h-full ${
              data.isHardLimitReached
                ? 'bg-rose-500'
                : data.isSoftLimitReached
                  ? 'bg-amber-500'
                  : 'bg-emerald-500'
            }`}
            style={{ width: `${Math.min(100, Math.max(0, data.usagePercent))}%` }}
          />
        </div>
        <span className="text-[11px] font-semibold">
          {t('credit.usage', { percent: data.usagePercent.toFixed(0) })}
        </span>
      </div>
      {data.isHardLimitReached ? (
        <p className="mt-1 text-[11px]">{t('credit.blocked')}</p>
      ) : data.isSoftLimitReached ? (
        <p className="mt-1 text-[11px]">{t('credit.warning')}</p>
      ) : null}
    </div>
  );
};
