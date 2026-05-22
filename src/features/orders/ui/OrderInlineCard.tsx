import { useTranslation } from 'react-i18next';
import { InlineDetailCard } from '@/shared/ui/InlineDetailCard/InlineDetailCard';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import { useOrderQuery } from '@/features/orders/hooks/useOrderQueries';

interface OrderInlineCardProps {
  orderId: string;
  fallbackTitle?: string;
  onClose: () => void;
  onOpenPanel: () => void;
}

export const OrderInlineCard = ({
  orderId,
  fallbackTitle,
  onClose,
  onOpenPanel,
}: OrderInlineCardProps) => {
  const { i18n } = useTranslation();
  const locale = i18n.language;
  const orderQuery = useOrderQuery(orderId);
  const order = orderQuery.data?.data;
  const currency = order?.currency ?? 'TRY';

  return (
    <InlineDetailCard
      title={order?.orderNumber ?? fallbackTitle ?? 'Sipariş'}
      subtitle={order ? `${order.customerName} · ${order.status}` : undefined}
      onOpenPanel={onOpenPanel}
      onClose={onClose}
    >
      {orderQuery.isPending ? (
        <div className="py-6 text-center text-sm text-slate-500 dark:text-slate-400">
          Yükleniyor…
        </div>
      ) : !order ? (
        <div className="py-6 text-center text-sm text-slate-500 dark:text-slate-400">
          Sipariş bulunamadı.
        </div>
      ) : (
        <div className="space-y-4">
          <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                <tr>
                  <th className="px-2 py-1.5 text-left">#</th>
                  <th className="px-2 py-1.5 text-left">Ürün</th>
                  <th className="px-2 py-1.5 text-right">Miktar</th>
                  <th className="px-2 py-1.5 text-right">Birim Fiyat</th>
                  <th className="px-2 py-1.5 text-right">İsk %</th>
                  <th className="px-2 py-1.5 text-right">KDV %</th>
                  <th className="px-2 py-1.5 text-right">Tutar</th>
                </tr>
              </thead>
              <tbody>
                {order.lines.map((l) => (
                  <tr key={l.id} className="border-t border-slate-100 dark:border-slate-800">
                    <td className="px-2 py-1.5 text-slate-500 dark:text-slate-400">
                      {l.lineNumber}
                    </td>
                    <td className="px-2 py-1.5">
                      <div className="font-medium text-slate-900 dark:text-slate-100">
                        {l.productName}
                      </div>
                      <div className="font-mono text-[10px] text-slate-400 dark:text-slate-500">
                        {l.productSku}
                      </div>
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono">
                      {formatNumber(l.quantity, locale)} {l.uomCode ?? ''}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono">
                      {formatCurrency(l.unitPrice, locale, currency)}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono text-slate-500 dark:text-slate-400">
                      {l.lineDiscountPercent > 0 ? `${l.lineDiscountPercent}` : '—'}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono text-slate-500 dark:text-slate-400">
                      {l.taxRatePercent}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono font-semibold">
                      {formatCurrency(l.lineTotal, locale, currency)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex flex-col items-end gap-0.5 text-xs">
            <Row label="Ara Toplam" value={formatCurrency(order.subtotal, locale, currency)} />
            {order.lineDiscountTotal > 0 && (
              <Row
                label="İskonto"
                value={`-${formatCurrency(order.lineDiscountTotal, locale, currency)}`}
              />
            )}
            <Row label="KDV" value={formatCurrency(order.taxTotal, locale, currency)} />
            {order.withholdingTotal > 0 && (
              <Row
                label="Tevkifat"
                value={`-${formatCurrency(order.withholdingTotal, locale, currency)}`}
              />
            )}
            {order.shippingCost > 0 && (
              <Row label="Kargo" value={formatCurrency(order.shippingCost, locale, currency)} />
            )}
            <div className="mt-1 border-t border-slate-200 pt-1 text-sm font-bold text-slate-900 dark:border-slate-700 dark:text-slate-100">
              Genel Toplam: {formatCurrency(order.total, locale, currency)}
            </div>
          </div>
        </div>
      )}
    </InlineDetailCard>
  );
};

const Row = ({ label, value }: { label: string; value: string }) => (
  <div className="flex w-48 justify-between">
    <span className="text-slate-500 dark:text-slate-400">{label}</span>
    <span className="font-mono text-slate-700 dark:text-slate-200">{value}</span>
  </div>
);
