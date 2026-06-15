import { useTranslation } from 'react-i18next';

interface FxRateSnapshotLabelProps {
  currencyCode: string;
  fxRateSnapshot?: number | null;
  fxSource?: string | null;
  fxLockedAtUtc?: string | null;
  baseCurrencyCode?: string;
}

const formatRate = (value: number): string =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(
    value,
  );

const formatDate = (iso: string): string => {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString();
};

export const FxRateSnapshotLabel = ({
  currencyCode,
  fxRateSnapshot,
  fxSource,
  fxLockedAtUtc,
  baseCurrencyCode = 'TRY',
}: FxRateSnapshotLabelProps) => {
  const { t } = useTranslation();

  if (fxRateSnapshot === null || fxRateSnapshot === undefined || !fxSource || !fxLockedAtUtc) {
    return (
      <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-1 text-xs text-slate-500 dark:bg-slate-800 dark:text-slate-400">
        {t('Fx.Snapshot.NotLocked', 'No FX lock')}
      </span>
    );
  }

  return (
    <span
      className="inline-flex items-center gap-1 rounded-md bg-indigo-50 px-2 py-1 text-xs font-medium text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-200"
      title={`${t('Fx.Snapshot.LockedAt', 'Locked at')}: ${new Date(fxLockedAtUtc).toLocaleString()}`}
    >
      {currencyCode} {formatRate(fxRateSnapshot)} {baseCurrencyCode}
      <span className="text-indigo-500 dark:text-indigo-300">·</span>
      {fxSource} {t('Fx.Snapshot.On', 'on')} {formatDate(fxLockedAtUtc)}
    </span>
  );
};
