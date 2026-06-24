import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ArrowDownLeft, ArrowUpRight, ExternalLink, FileMinus } from 'lucide-react';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import { fmtCurrency } from './ledgerModel';

export const LinkedDocumentsCard = ({
  invoice,
  creditNotes,
  locale,
}: {
  invoice: Invoice;
  creditNotes: Array<{
    id: string;
    invoiceNumber: string;
    total: number;
    currency: string;
    issueDate: string;
    status: string;
  }>;
  locale: string;
}) => {
  const { t } = useTranslation();
  const hasLinks = !!invoice.orderId || creditNotes.length > 0 || !!invoice.originInvoiceId;
  if (!hasLinks) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <FileMinus size={12} />
          {t('invoices.ledger.linkedDocs')}
        </span>
        <span className="text-slate-400">
          {(invoice.orderId ? 1 : 0) + creditNotes.length + (invoice.originInvoiceId ? 1 : 0)}
        </span>
      </header>
      <ul className="mt-2 space-y-1">
        {invoice.orderId && (
          <li>
            <Link
              to={`/dashboard/orders?selected=${invoice.orderId}`}
              className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 text-[11px] transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <span className="inline-flex items-center gap-1.5">
                <ArrowUpRight size={11} className="text-primary-500" />
                <span className="text-slate-700 dark:text-slate-300">
                  {t('invoices.detail.linkedOrder')}
                </span>
              </span>
              <ExternalLink size={10} className="text-slate-400" />
            </Link>
          </li>
        )}
        {invoice.originInvoiceId && (
          <li>
            <Link
              to={`/dashboard/invoices?selected=${invoice.originInvoiceId}`}
              className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 text-[11px] transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <span className="inline-flex items-center gap-1.5">
                <ArrowUpRight size={11} className="text-primary-500" />
                <span className="text-slate-700 dark:text-slate-300">
                  {t('invoices.ledger.originInvoice')}
                </span>
              </span>
              <ExternalLink size={10} className="text-slate-400" />
            </Link>
          </li>
        )}
        {creditNotes.map((cn) => (
          <li key={cn.id}>
            <Link
              to={`/dashboard/invoices?selected=${cn.id}`}
              className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 text-[11px] transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <span className="inline-flex items-center gap-1.5">
                <ArrowDownLeft size={11} className="text-danger-500" />
                <span className="font-mono text-slate-900 dark:text-slate-100">
                  {cn.invoiceNumber}
                </span>
                <span className="rounded bg-danger-100 px-1 text-[9px] font-semibold uppercase text-danger-700 dark:bg-danger-500/20 dark:text-danger-300">
                  {t('invoices.ledger.creditNote')}
                </span>
              </span>
              <span className="font-mono tabular-nums text-slate-700 dark:text-slate-300">
                {fmtCurrency(cn.total, cn.currency, locale)}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
};
