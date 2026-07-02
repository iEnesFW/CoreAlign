import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Banknote, Landmark, PiggyBank, Wallet } from 'lucide-react';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { useCashPositionQuery } from '@/features/reports/hooks/useReportQueries';

export const CashPositionPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [asOf, setAsOf] = useState('');

  const asOfUtc = asOf ? new Date(`${asOf}T23:59:59Z`).toISOString() : undefined;
  const query = useCashPositionQuery(asOfUtc ? { asOfUtc } : {});
  const data = query.data?.data;
  const currency = data?.currency ?? 'TRY';

  const cards = [
    {
      key: 'cashOnHand',
      label: t('CashPosition.cashOnHand', { defaultValue: 'Kasa (100)' }),
      value: data?.cashOnHand ?? 0,
      icon: <Wallet size={16} />,
      tone: 'text-success-600 dark:text-success-400',
    },
    {
      key: 'bankBalance',
      label: t('CashPosition.bankBalance', { defaultValue: 'Bankalar (102)' }),
      value: data?.bankBalance ?? 0,
      icon: <Landmark size={16} />,
      tone: 'text-primary-600 dark:text-primary-400',
    },
    {
      key: 'totalCash',
      label: t('CashPosition.totalCash', { defaultValue: 'Toplam Nakit' }),
      value: data?.totalCash ?? 0,
      icon: <PiggyBank size={16} />,
      tone: 'text-slate-900 dark:text-slate-100',
    },
    {
      key: 'customerAdvances',
      label: t('CashPosition.customerAdvances', { defaultValue: 'Müşteri Avansları (340)' }),
      value: data?.customerAdvances ?? 0,
      icon: <Banknote size={16} />,
      tone: 'text-warning-600 dark:text-warning-400',
    },
  ];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<PiggyBank size={20} />}
          title={t('CashPosition.title', { defaultValue: 'Nakit Pozisyon' })}
          subtitle={t('CashPosition.subtitle', {
            defaultValue: 'Kasa + banka GL bakiyeleri (TDHP 100/102) ile anlık nakit durumu.',
          })}
          actions={
            <label className="inline-flex items-center gap-1.5 text-[11px] text-slate-500 dark:text-slate-400">
              {t('CashPosition.asOf', { defaultValue: 'Tarih' })}
              <input
                type="date"
                value={asOf}
                onChange={(e) => setAsOf(e.target.value)}
                className="rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
            </label>
          }
        />
      }
    >
      {query.isPending ? (
        <div className="px-3 py-8 text-center text-sm text-slate-500">
          {t('common.loading', { defaultValue: 'Yükleniyor…' })}
        </div>
      ) : (
        <div className="space-y-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
            {cards.map((c) => (
              <div
                key={c.key}
                className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900"
              >
                <div className="flex items-center gap-1.5 text-[11px] font-medium uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {c.icon}
                  {c.label}
                </div>
                <div className={`mt-1.5 font-mono text-xl font-bold ${c.tone}`}>
                  {formatCurrency(c.value, locale, currency)}
                </div>
              </div>
            ))}
          </div>

          <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
            <div className="bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-700 dark:bg-slate-900/40 dark:text-slate-200">
              {t('CashPosition.accountsTitle', { defaultValue: 'Banka Hesapları' })}
            </div>
            {(data?.accounts.length ?? 0) === 0 ? (
              <div className="px-3 py-6 text-center text-sm text-slate-500">
                {t('CashPosition.noAccounts', { defaultValue: 'Tanımlı banka hesabı yok.' })}
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
                  <tr>
                    <th className="px-3 py-2 text-left">
                      {t('CashPosition.cols.account', { defaultValue: 'Hesap' })}
                    </th>
                    <th className="px-3 py-2 text-left">
                      {t('CashPosition.cols.bank', { defaultValue: 'Banka' })}
                    </th>
                    <th className="px-3 py-2 text-left">
                      {t('CashPosition.cols.iban', { defaultValue: 'IBAN' })}
                    </th>
                    <th className="px-3 py-2 text-right">
                      {t('CashPosition.cols.opening', { defaultValue: 'Açılış Bakiyesi' })}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                  {data?.accounts.map((a) => (
                    <tr key={a.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                      <td className="px-3 py-2 text-slate-800 dark:text-slate-100">
                        {a.accountName}
                        {a.isPrimary && (
                          <span className="ml-1.5 inline-flex rounded bg-primary-100 px-1.5 text-[10px] font-medium text-primary-700 dark:bg-primary-500/20 dark:text-primary-300">
                            {t('CashPosition.primary', { defaultValue: 'Birincil' })}
                          </span>
                        )}
                      </td>
                      <td className="px-3 py-2 text-slate-600 dark:text-slate-400">{a.bankName}</td>
                      <td className="px-3 py-2 font-mono text-[11px] text-slate-500">{a.iban}</td>
                      <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                        {formatCurrency(a.openingBalance, locale, a.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}
    </ListPageTemplate>
  );
};

export default CashPositionPage;
