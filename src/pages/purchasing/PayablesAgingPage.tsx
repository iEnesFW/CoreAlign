import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useVendorAgingQuery } from '@/features/purchasing/hooks/useVendorBilling';
import type { VendorAgingRow } from '@/features/purchasing/model/vendorBilling.types';

const BUCKETS: {
  key: keyof Pick<
    VendorAgingRow,
    'current' | 'days1To30' | 'days31To60' | 'days61To90' | 'daysOver90'
  >;
  labelKey: string;
  defaultValue: string;
  tone: string;
}[] = [
  {
    key: 'current',
    labelKey: 'apAging.current',
    defaultValue: 'Güncel',
    tone: 'text-slate-700 dark:text-slate-300',
  },
  {
    key: 'days1To30',
    labelKey: 'apAging.b1',
    defaultValue: '1-30 gün',
    tone: 'text-amber-700 dark:text-amber-300',
  },
  {
    key: 'days31To60',
    labelKey: 'apAging.b2',
    defaultValue: '31-60 gün',
    tone: 'text-amber-700 dark:text-amber-300',
  },
  {
    key: 'days61To90',
    labelKey: 'apAging.b3',
    defaultValue: '61-90 gün',
    tone: 'text-orange-700 dark:text-orange-300',
  },
  {
    key: 'daysOver90',
    labelKey: 'apAging.b4',
    defaultValue: '90+ gün',
    tone: 'text-rose-700 dark:text-rose-300',
  },
];

export const PayablesAgingPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const query = useVendorAgingQuery();
  const rows = query.data?.data ?? [];

  const sum = (key: keyof VendorAgingRow) => rows.reduce((acc, r) => acc + (r[key] as number), 0);

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('apAging.title', { defaultValue: 'Borç Yaşlandırma' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('apAging.subtitle', {
              defaultValue: 'Tedarikçilere olan açık borçların vade yaşına göre dağılımı.',
            })}
          </p>
        </div>
        <Link
          to="/dashboard/vendor-bills"
          className="text-xs text-indigo-600 hover:underline dark:text-indigo-400"
        >
          {t('apAging.toBills', { defaultValue: 'Tedarikçi Faturaları →' })}
        </Link>
      </div>

      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('apAging.vendor', { defaultValue: 'Tedarikçi' })}
              </th>
              {BUCKETS.map((b) => (
                <th key={b.key} className="px-3 py-2 text-right">
                  {t(b.labelKey, { defaultValue: b.defaultValue })}
                </th>
              ))}
              <th className="px-3 py-2 text-right">
                {t('apAging.total', { defaultValue: 'Toplam' })}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {query.isPending ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('common.loading', { defaultValue: 'Yükleniyor…' })}
                </td>
              </tr>
            ) : query.isError ? (
              <tr>
                <td
                  colSpan={7}
                  className="px-3 py-8 text-center text-sm text-rose-600 dark:text-rose-400"
                >
                  {t('apAging.error', { defaultValue: 'Rapor yüklenemedi.' })}
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td
                  colSpan={7}
                  className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400"
                >
                  {t('apAging.empty', { defaultValue: 'Açık tedarikçi borcu bulunmuyor.' })}
                </td>
              </tr>
            ) : (
              rows.map((r) => (
                <tr key={r.vendorId} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{r.vendorName}</td>
                  {BUCKETS.map((b) => (
                    <td key={b.key} className={`px-3 py-2 text-right font-mono ${b.tone}`}>
                      {r[b.key] > 0 ? formatCurrency(r[b.key], locale, r.currency) : '—'}
                    </td>
                  ))}
                  <td className="px-3 py-2 text-right font-mono font-semibold text-slate-900 dark:text-slate-100">
                    {formatCurrency(r.total, locale, r.currency)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
          {rows.length > 0 && (
            <tfoot className="border-t-2 border-slate-200 bg-slate-50/60 font-semibold dark:border-slate-700 dark:bg-slate-900/30">
              <tr>
                <td className="px-3 py-2 text-slate-700 dark:text-slate-200">
                  {t('apAging.grandTotal', { defaultValue: 'Genel Toplam' })}
                </td>
                {BUCKETS.map((b) => (
                  <td
                    key={b.key}
                    className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200"
                  >
                    {formatCurrency(sum(b.key), locale)}
                  </td>
                ))}
                <td className="px-3 py-2 text-right font-mono text-slate-900 dark:text-slate-100">
                  {formatCurrency(sum('total'), locale)}
                </td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </div>
  );
};

export default PayablesAgingPage;
