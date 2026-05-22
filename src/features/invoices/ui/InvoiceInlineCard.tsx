import { useTranslation } from 'react-i18next';
import { InlineDetailCard } from '@/shared/ui/InlineDetailCard/InlineDetailCard';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import { useInvoiceQuery } from '@/features/invoices/hooks/useInvoiceQueries';

interface InvoiceInlineCardProps {
  invoiceId: string;
  onClose: () => void;
  onOpenPanel: () => void;
}

export const InvoiceInlineCard = ({ invoiceId, onClose, onOpenPanel }: InvoiceInlineCardProps) => {
  const { i18n } = useTranslation();
  const locale = i18n.language;
  const invoiceQuery = useInvoiceQuery(invoiceId);
  const inv = invoiceQuery.data?.data;
  const currency = inv?.currency ?? 'TRY';

  return (
    <InlineDetailCard
      title={inv?.invoiceNumber ?? 'Fatura'}
      subtitle={inv ? `${inv.customerName} · ${inv.status}` : undefined}
      onOpenPanel={onOpenPanel}
      onClose={onClose}
    >
      {invoiceQuery.isPending ? (
        <div className="py-6 text-center text-sm text-slate-500">Yükleniyor…</div>
      ) : !inv ? (
        <div className="py-6 text-center text-sm text-slate-500">Fatura bulunamadı.</div>
      ) : (
        <div className="space-y-4">
          <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                <tr>
                  <th className="px-2 py-1.5 text-left">#</th>
                  <th className="px-2 py-1.5 text-left">Ürün/Hizmet</th>
                  <th className="px-2 py-1.5 text-right">Miktar</th>
                  <th className="px-2 py-1.5 text-right">Birim Fiyat</th>
                  <th className="px-2 py-1.5 text-right">KDV %</th>
                  <th className="px-2 py-1.5 text-right">Tutar</th>
                </tr>
              </thead>
              <tbody>
                {inv.lines.map((l) => (
                  <tr key={l.id} className="border-t border-slate-100 dark:border-slate-800">
                    <td className="px-2 py-1.5 text-slate-500">{l.lineNumber}</td>
                    <td className="px-2 py-1.5">
                      <div className="font-medium text-slate-900 dark:text-slate-100">
                        {l.productName}
                      </div>
                      {l.productSku && (
                        <div className="font-mono text-[10px] text-slate-400">{l.productSku}</div>
                      )}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono">
                      {formatNumber(l.quantity, locale)} {l.uomCode ?? ''}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono">
                      {formatCurrency(l.unitPrice, locale, currency)}
                    </td>
                    <td className="px-2 py-1.5 text-right font-mono text-slate-500">
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
            <Row label="Ara Toplam" value={formatCurrency(inv.subtotal, locale, currency)} />
            {inv.lineDiscountTotal > 0 && (
              <Row
                label="İskonto"
                value={`-${formatCurrency(inv.lineDiscountTotal, locale, currency)}`}
              />
            )}
            <Row label="KDV" value={formatCurrency(inv.taxTotal, locale, currency)} />
            {inv.withholdingTotal > 0 && (
              <Row
                label="Tevkifat"
                value={`-${formatCurrency(inv.withholdingTotal, locale, currency)}`}
              />
            )}
            <div className="mt-1 border-t border-slate-200 pt-1 text-sm font-bold text-slate-900 dark:border-slate-700 dark:text-slate-100">
              Genel Toplam: {formatCurrency(inv.total, locale, currency)}
            </div>
            <Row label="Ödenen" value={formatCurrency(inv.amountPaid, locale, currency)} />
            <Row
              label="Kalan"
              value={formatCurrency(inv.amountDue, locale, currency)}
              emphasize={inv.amountDue > 0}
            />
          </div>
        </div>
      )}
    </InlineDetailCard>
  );
};

const Row = ({
  label,
  value,
  emphasize,
}: {
  label: string;
  value: string;
  emphasize?: boolean;
}) => (
  <div className="flex w-48 justify-between">
    <span className="text-slate-500">{label}</span>
    <span
      className={`font-mono ${emphasize ? 'font-semibold text-rose-600 dark:text-rose-400' : 'text-slate-700 dark:text-slate-200'}`}
    >
      {value}
    </span>
  </div>
);
