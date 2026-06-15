import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { RefreshCw } from 'lucide-react';
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
    <section className="space-y-4 p-4">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('Settings.ExchangeRates.Title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Settings.ExchangeRates.Subtitle')}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <label className="text-sm">
            <span className="mr-2 text-slate-600 dark:text-slate-300">
              {t('Settings.ExchangeRates.CurrencyFilter')}
            </span>
            <input
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              placeholder="USD"
              className="w-24 rounded border border-slate-200 bg-white p-1 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </label>
          <button
            type="button"
            onClick={() => refresh.mutate()}
            disabled={refresh.isPending}
            className="inline-flex items-center gap-1 rounded bg-indigo-600 px-3 py-1.5 text-sm text-white hover:bg-indigo-700 disabled:opacity-60"
          >
            <RefreshCw size={14} className={refresh.isPending ? 'animate-spin' : ''} />
            {t('Settings.ExchangeRates.Refresh')}
          </button>
        </div>
      </header>

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
    </section>
  );
}

export default ExchangeRatesPage;
