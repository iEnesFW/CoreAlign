import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, CheckCircle2, ListChecks } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency } from '@/shared/lib/format';
import { useThreeWayMatchQuery } from '@/features/purchasing/hooks/useVendorBilling';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';

const ThreeWayMatchReport = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [vendorId, setVendorId] = useState('');
  const [fromUtc, setFromUtc] = useState('');
  const [toUtc, setToUtc] = useState('');

  const vendors = useVendorsQuery({ page: 1, pageSize: 200 });
  const rows = useThreeWayMatchQuery({
    vendorId: vendorId || undefined,
    fromUtc: fromUtc || undefined,
    toUtc: toUtc || undefined,
  });
  const items = rows.data?.data ?? [];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<ListChecks size={20} />}
          title={t('VendorBills.threeWayMatch.title', { defaultValue: '3-Way Match Raporu' })}
          subtitle={t('VendorBills.threeWayMatch.subtitle', {
            defaultValue:
              'Sipariş ↔ Mal Kabul ↔ Fatura uyumsuzluklarını listeler. Discrepancy kodlarına göre filtreleyin.',
          })}
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={vendorId}
            onChange={(e) => setVendorId(e.target.value)}
            className="w-full sm:w-56"
          >
            <option value="">
              {t('VendorBills.threeWayMatch.allVendors', { defaultValue: 'Tüm tedarikçiler' })}
            </option>
            {(vendors.data?.data?.items ?? []).map((v) => (
              <option key={v.id} value={v.id}>
                {v.name}
              </option>
            ))}
          </Select>
          <Input
            type="date"
            value={fromUtc}
            onChange={(e) => setFromUtc(e.target.value)}
            className="w-full sm:w-44"
          />
          <Input
            type="date"
            value={toUtc}
            onChange={(e) => setToUtc(e.target.value)}
            className="w-full sm:w-44"
          />
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('VendorBills.threeWayMatch.count', {
              defaultValue: '{{count}} satır',
              count: items.length,
            })}
          </span>
        </div>
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {rows.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('VendorBills.threeWayMatch.empty', {
              defaultValue: 'Tüm sipariş-mal kabul-fatura zincirleri uyumlu.',
            })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('VendorBills.threeWayMatch.cols.po', { defaultValue: 'PO No' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('VendorBills.threeWayMatch.cols.vendor', { defaultValue: 'Tedarikçi' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('VendorBills.threeWayMatch.cols.product', { defaultValue: 'Ürün' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('VendorBills.threeWayMatch.cols.expected', { defaultValue: 'Beklenen' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('VendorBills.threeWayMatch.cols.received', { defaultValue: 'Gelen' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('VendorBills.threeWayMatch.cols.billed', { defaultValue: 'Fatura' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('VendorBills.threeWayMatch.cols.billedAmount', {
                    defaultValue: 'Fatura Tutarı',
                  })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('VendorBills.threeWayMatch.cols.match', { defaultValue: 'Eşleşme' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('VendorBills.threeWayMatch.cols.flags', { defaultValue: 'Uyumsuzluklar' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((r, idx) => (
                <tr
                  key={`${r.purchaseOrderId}-${r.productId}-${idx}`}
                  className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {r.poNumber}
                  </td>
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{r.vendorName}</td>
                  <td className="px-3 py-2 text-xs text-slate-700 dark:text-slate-200">
                    <div className="font-medium">{r.productName}</div>
                    <div className="text-[10px] text-slate-500">{r.productSku}</div>
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {r.expectedQty.toFixed(2)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {r.receivedQty.toFixed(2)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {r.billedQty.toFixed(2)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {formatCurrency(r.billedAmount, locale, r.currency)}
                  </td>
                  <td className="px-3 py-2 text-center">
                    {r.discrepancies.length === 0 ? (
                      <Badge variant="success" className="gap-1">
                        <CheckCircle2 size={10} />
                        {t('VendorBills.threeWayMatch.matched', { defaultValue: 'Uyumlu' })}
                      </Badge>
                    ) : (
                      <Badge variant="danger" className="gap-1">
                        <AlertTriangle size={10} />
                        {t('VendorBills.threeWayMatch.mismatch', { defaultValue: 'Uyumsuz' })}
                      </Badge>
                    )}
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex flex-wrap items-center gap-1">
                      {r.discrepancies.map((code) => (
                        <Badge key={code} variant="danger" className="gap-1">
                          <AlertTriangle size={10} />
                          {t(`VendorBills.threeWayMatch.codes.${code}`, { defaultValue: code })}
                        </Badge>
                      ))}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </ListPageTemplate>
  );
};

export default ThreeWayMatchReport;
export { ThreeWayMatchReport };
