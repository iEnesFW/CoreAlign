import { useTranslation } from 'react-i18next';
import { Ban, ArrowRightLeft } from 'lucide-react';
import { formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
import type { IncomingInvoiceDto, IncomingInvoiceStatus } from '../model/incomingInvoice.types';

const STATUS_VARIANT: Record<IncomingInvoiceStatus, BadgeVariant> = {
  New: 'info',
  Reviewed: 'warning',
  Processed: 'success',
  Ignored: 'neutral',
};

interface Props {
  items: IncomingInvoiceDto[];
  isLoading: boolean;
  busy: boolean;
  onProcess: (invoice: IncomingInvoiceDto) => void;
  onIgnore: (invoice: IncomingInvoiceDto) => void;
}

export const IncomingInvoiceList = ({ items, isLoading, busy, onProcess, onIgnore }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const statusLabel = (s: IncomingInvoiceStatus) =>
    t(`incomingInvoices.status.${s}` as const, { defaultValue: s });

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
      {isLoading ? (
        <div className="px-3 py-8 text-center text-sm text-slate-500 dark:text-slate-400">
          {t('common.loading', { defaultValue: 'Yükleniyor…' })}
        </div>
      ) : items.length === 0 ? (
        <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
          <p>{t('incomingInvoices.empty', { defaultValue: 'Gelen fatura bulunamadı.' })}</p>
          <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">
            {t('incomingInvoices.emptyHint', {
              defaultValue: 'e-Fatura sağlayıcısından gelen faturalar burada listelenir.',
            })}
          </p>
        </div>
      ) : (
        <table className="w-full text-sm">
          <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('incomingInvoices.columns.senderVkn', { defaultValue: 'Gönderen VKN' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('incomingInvoices.columns.sender', { defaultValue: 'Gönderen' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('incomingInvoices.columns.invoiceNumber', { defaultValue: 'Fatura No' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('incomingInvoices.columns.issueDate', { defaultValue: 'Tarih' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('incomingInvoices.columns.provider', { defaultValue: 'Sağlayıcı' })}
              </th>
              <th className="px-3 py-2 text-center">
                {t('incomingInvoices.columns.status', { defaultValue: 'Durum' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('incomingInvoices.columns.actions', { defaultValue: 'İşlemler' })}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {items.map((row) => {
              const actionable = row.status === 'New' || row.status === 'Reviewed';
              return (
                <tr key={row.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {row.senderVkn}
                  </td>
                  <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                    {row.senderName ?? '—'}
                  </td>
                  <td className="px-3 py-2 text-slate-700 dark:text-slate-300">
                    {row.invoiceNumber}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {formatDate(row.issueDate, locale)}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {row.providerName}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge variant={STATUS_VARIANT[row.status]}>{statusLabel(row.status)}</Badge>
                  </td>
                  <td className="px-3 py-2 text-right">
                    <div className="inline-flex items-center gap-1">
                      {actionable && (
                        <button
                          type="button"
                          onClick={() => onProcess(row)}
                          disabled={busy}
                          className="rounded p-1 text-success-600 hover:bg-success-50 disabled:opacity-40 dark:text-success-300 dark:hover:bg-success-500/10"
                          title={t('incomingInvoices.actions.process', {
                            defaultValue: 'Sisteme İşle',
                          })}
                          aria-label={t('incomingInvoices.actions.process', {
                            defaultValue: 'Sisteme İşle',
                          })}
                        >
                          <ArrowRightLeft size={14} />
                        </button>
                      )}
                      {actionable && (
                        <button
                          type="button"
                          onClick={() => onIgnore(row)}
                          disabled={busy}
                          className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-40 dark:hover:bg-danger-500/10"
                          title={t('incomingInvoices.actions.ignore', { defaultValue: 'Yoksay' })}
                          aria-label={t('incomingInvoices.actions.ignore', {
                            defaultValue: 'Yoksay',
                          })}
                        >
                          <Ban size={14} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
};
