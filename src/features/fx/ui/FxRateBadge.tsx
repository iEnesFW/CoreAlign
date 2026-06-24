import { useTranslation } from 'react-i18next';
import { useFxRateQuery } from '../hooks/useFxRates';

interface FxRateBadgeProps {
  currencyCode: string;
  asOfDate?: string;
  baseCurrencyCode?: string;
}

const formatRate = (value: number): string =>
  new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(
    value,
  );

const formatDate = (iso: string): string => {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString();
};

export const FxRateBadge = ({
  currencyCode,
  asOfDate,
  baseCurrencyCode = 'TRY',
}: FxRateBadgeProps) => {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useFxRateQuery(currencyCode, asOfDate);

  if (isLoading) {
    return (
      <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-1 text-xs text-slate-500 dark:bg-slate-800 dark:text-slate-400">
        {t('Fx.LatestRates.Loading', 'Loading rate...')}
      </span>
    );
  }

  if (isError || !data) {
    return (
      <span className="inline-flex items-center gap-1 rounded-md bg-warning-100 px-2 py-1 text-xs text-warning-700 dark:bg-warning-900/30 dark:text-warning-300">
        {t('Fx.LatestRates.Unavailable', 'FX unavailable')}
      </span>
    );
  }

  return (
    <span
      className="inline-flex items-center gap-1 rounded-md bg-success-50 px-2 py-1 text-xs font-medium text-success-700 dark:bg-success-900/30 dark:text-success-300"
      title={`${t('Fx.LatestRates.LastUpdated', 'Last updated')}: ${formatDate(data.effectiveDate)} (${data.source})`}
    >
      1 {data.currencyCode} = {formatRate(data.buyingRate)} {baseCurrencyCode}
    </span>
  );
};
