import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { CheckCircle2, Eye, Printer, XCircle } from 'lucide-react';
import type { InvoiceStatus, InvoiceSummary } from '../model/invoice.types';

interface Props {
  invoices: InvoiceSummary[];
  isLoading: boolean;
  selectedId?: string | null;
  onView: (invoice: InvoiceSummary) => void;
  onMarkPaid: (invoice: InvoiceSummary) => void;
  onCancel: (invoice: InvoiceSummary) => void;
}

const statusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Issued: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
  Sent: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  PartiallyPaid: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Paid: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Overdue: 'bg-red-100 text-red-800 dark:bg-red-500/20 dark:text-red-300',
  Void: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Cancelled: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
};

const formatCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const formatDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

export const InvoiceList = ({
  invoices,
  isLoading,
  selectedId,
  onView,
  onMarkPaid,
  onCancel,
}: Props) => {
  const { t, i18n } = useTranslation();

  if (isLoading && invoices.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('common.loading')}
      </div>
    );
  }

  if (invoices.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('invoices.empty')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <Th>{t('invoices.columns.invoiceNumber')}</Th>
              <Th>{t('invoices.columns.customer')}</Th>
              <Th>{t('invoices.columns.issueDate')}</Th>
              <Th>{t('invoices.columns.dueDate')}</Th>
              <Th>{t('invoices.columns.status')}</Th>
              <Th>{t('invoices.columns.total')}</Th>
              <th className="px-3 py-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.columns.actions')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {invoices.map((invoice) => {
              const isSelected = selectedId === invoice.id;
              return (
                <tr
                  key={invoice.id}
                  onClick={() => onView(invoice)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onView(invoice);
                    }
                  }}
                  tabIndex={0}
                  role="button"
                  aria-selected={isSelected}
                  className={`cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-500 ${
                    isSelected
                      ? 'bg-indigo-50 dark:bg-indigo-500/10'
                      : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'
                  }`}
                >
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-200">
                    {invoice.invoiceNumber}
                  </td>
                  <td className="px-3 py-2 font-medium text-slate-900 dark:text-slate-100">
                    {invoice.customerName}
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                    {formatDate(invoice.issueDate, i18n.language)}
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
                    {formatDate(invoice.dueDate, i18n.language)}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${statusStyles[invoice.status]}`}
                    >
                      {t(`invoices.status.${invoice.status}` as never)}
                    </span>
                  </td>
                  <td className="px-3 py-2 font-semibold text-slate-900 dark:text-slate-100">
                    {formatCurrency(invoice.total, invoice.currency, i18n.language)}
                  </td>
                  <td className="px-3 py-2 text-right" onClick={(e) => e.stopPropagation()}>
                    <div className="inline-flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => onView(invoice)}
                        className="rounded p-1.5 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                        aria-label={t('common.view')}
                      >
                        <Eye size={14} />
                      </button>
                      <Link
                        to={`/invoices/${invoice.id}/print`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="rounded p-1.5 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                        aria-label={t('invoices.actions.print')}
                        title={t('invoices.actions.print')}
                      >
                        <Printer size={14} />
                      </Link>
                      {invoice.status === 'Issued' && (
                        <button
                          type="button"
                          onClick={() => onMarkPaid(invoice)}
                          className="rounded p-1.5 text-slate-500 hover:bg-emerald-50 hover:text-emerald-600 dark:text-slate-400 dark:hover:bg-emerald-500/10 dark:hover:text-emerald-400"
                          aria-label={t('invoices.actions.markPaid')}
                          title={t('invoices.actions.markPaid')}
                        >
                          <CheckCircle2 size={14} />
                        </button>
                      )}
                      {(invoice.status === 'Draft' || invoice.status === 'Issued') && (
                        <button
                          type="button"
                          onClick={() => onCancel(invoice)}
                          className="rounded p-1.5 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:text-slate-400 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                          aria-label={t('invoices.actions.cancel')}
                          title={t('invoices.actions.cancel')}
                        >
                          <XCircle size={14} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const Th = ({ children }: { children: React.ReactNode }) => (
  <th className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
    {children}
  </th>
);
