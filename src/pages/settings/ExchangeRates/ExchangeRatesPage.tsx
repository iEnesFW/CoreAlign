import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Coins, RefreshCw } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import {
  useExchangeRatesQuery,
  useRefreshExchangeRates,
} from '@/features/exchange-rates/useExchangeRates';

export function ExchangeRatesPage() {
  const { t, i18n } = useTranslation();
  const [currency, setCurrency] = useState<string>('');
  const query = useExchangeRatesQuery(undefined, undefined, currency || undefined);
  const refresh = useRefreshExchangeRates();
  const rates = query.data?.data ?? [];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Coins size={20} />}
          title={t('Settings.ExchangeRates.Title')}
          subtitle={t('Settings.ExchangeRates.Subtitle')}
          actions={
            <Button
              size="sm"
              onClick={() => refresh.mutate()}
              isLoading={refresh.isPending}
              disabled={refresh.isPending}
            >
              <RefreshCw size={14} className={refresh.isPending ? 'animate-spin' : ''} />
              {t('Settings.ExchangeRates.Refresh')}
            </Button>
          }
        />
      }
      toolbar={
        <Input
          label={t('Settings.ExchangeRates.CurrencyFilter')}
          value={currency}
          onChange={(e) => setCurrency(e.target.value.toUpperCase())}
          placeholder="USD"
          className="w-full sm:w-48"
        />
      }
    >
      <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
        <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-700">
          <thead className="bg-slate-50 dark:bg-slate-800/60">
            <tr>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Settings.ExchangeRates.Currency')}
              </th>
              <th className="px-3 py-2 text-right font-semibold text-slate-700 dark:text-slate-200">
                {t('Settings.ExchangeRates.RateAgainstTry')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Settings.ExchangeRates.ValidOn')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Settings.ExchangeRates.Source')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-900">
            {rates.map((rate) => (
              <tr key={rate.id}>
                <td className="px-3 py-2 font-mono text-slate-800 dark:text-slate-100">
                  {rate.currency}
                </td>
                <td className="px-3 py-2 text-right text-slate-800 dark:text-slate-100">
                  {rate.rateAgainstTry.toLocaleString(i18n.language, { maximumFractionDigits: 6 })}
                </td>
                <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                  {new Date(rate.validOnDate).toLocaleDateString(i18n.language)}
                </td>
                <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{rate.source}</td>
              </tr>
            ))}
            {rates.length === 0 && !query.isLoading && (
              <tr>
                <td
                  colSpan={4}
                  className="px-3 py-6 text-center text-sm text-slate-500 dark:text-slate-400"
                >
                  {t('Settings.ExchangeRates.Empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </ListPageTemplate>
  );
}

export default ExchangeRatesPage;
