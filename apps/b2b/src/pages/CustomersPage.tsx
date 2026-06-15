import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { useDealerCustomers } from '@/features/portal/hooks';

export const CustomersPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useDealerCustomers();

  return (
    <div className="space-y-6">
      <PageHeader title={t('b2b.customers.title')} subtitle={t('b2b.customers.subtitle')} />

      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="flex items-center gap-2 px-6 py-8 text-sm text-slate-500">
            <Spinner /> {t('b2b.common.loading')}
          </div>
        ) : (data?.length ?? 0) === 0 ? (
          <p className="px-6 py-8 text-sm text-slate-400">{t('b2b.customers.empty')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th className="px-6 py-3 font-medium">{t('b2b.customers.code')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.customers.name')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.customers.taxNumber')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.customers.currency')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.customers.priceList')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
                {data!.map((c) => (
                  <tr key={c.customerId}>
                    <td className="px-6 py-3 font-mono text-xs text-slate-500">{c.code ?? '—'}</td>
                    <td className="px-6 py-3 font-medium text-slate-900 dark:text-slate-100">
                      {c.name}
                    </td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {c.taxNumber ?? '—'}
                    </td>
                    <td className="px-6 py-3 text-slate-700 dark:text-slate-200">{c.currency}</td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {c.defaultPriceListName ?? '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
};
