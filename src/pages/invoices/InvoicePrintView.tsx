import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Printer } from 'lucide-react';
import { useInvoiceQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import { useAuthStore } from '@/features/auth/model/authStore';
import type { InvoiceStatus } from '@/features/invoices/model/invoice.types';

const statusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700',
  Issued: 'bg-blue-100 text-blue-700',
  Sent: 'bg-sky-100 text-sky-700',
  PartiallyPaid: 'bg-amber-100 text-amber-800',
  Paid: 'bg-emerald-100 text-emerald-700',
  Overdue: 'bg-red-100 text-red-800',
  Void: 'bg-rose-100 text-rose-700',
  Cancelled: 'bg-red-100 text-red-700',
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
    return new Intl.DateTimeFormat(locale, { dateStyle: 'long' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

export const InvoicePrintView = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const query = useInvoiceQuery(id ?? null);
  const invoice = query.data?.data;
  const tenantName = useAuthStore((s) => s.user?.tenantName ?? '');

  useEffect(() => {
    document.body.classList.add('bg-white');
    return () => document.body.classList.remove('bg-white');
  }, []);

  if (query.isPending || !invoice) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-white text-sm text-slate-500">
        {t('common.loading')}
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white text-slate-900">
      <div className="no-print sticky top-0 z-10 border-b border-slate-200 bg-white px-4 py-2 print:hidden">
        <div className="mx-auto flex max-w-3xl items-center justify-between">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-sm text-slate-700 hover:bg-slate-100"
          >
            <ArrowLeft size={16} />
            {t('common.back')}
          </button>
          <button
            type="button"
            onClick={() => window.print()}
            className="inline-flex items-center gap-1 rounded bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700"
          >
            <Printer size={14} />
            {t('invoices.print.button')}
          </button>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-8 py-10 print:px-0 print:py-0">
        <header className="flex items-start justify-between border-b border-slate-200 pb-6">
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              {t('invoices.print.from')}
            </div>
            <div className="mt-1 text-2xl font-bold text-slate-900">{tenantName}</div>
          </div>
          <div className="text-right">
            <div className="text-3xl font-bold tracking-tight text-slate-900">
              {t('invoices.print.heading')}
            </div>
            <div className="mt-1 font-mono text-sm text-slate-600">{invoice.invoiceNumber}</div>
            <span
              className={`mt-2 inline-flex rounded px-2 py-0.5 text-xs font-medium ${statusStyles[invoice.status]}`}
            >
              {t(`invoices.status.${invoice.status}` as never)}
            </span>
          </div>
        </header>

        <section className="mt-6 grid grid-cols-2 gap-6">
          <div>
            <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
              {t('invoices.print.billTo')}
            </div>
            <div className="mt-1 text-base font-semibold text-slate-900">
              {invoice.customerName}
            </div>
          </div>
          <div className="text-right">
            <Row
              label={t('invoices.fields.issueDate')}
              value={formatDate(invoice.issueDate, i18n.language)}
            />
            <Row
              label={t('invoices.fields.dueDate')}
              value={formatDate(invoice.dueDate, i18n.language)}
            />
            {invoice.paidAtUtc && (
              <Row
                label={t('invoices.fields.paidAt')}
                value={formatDate(invoice.paidAtUtc, i18n.language)}
              />
            )}
          </div>
        </section>

        <section className="mt-8">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b-2 border-slate-300">
                <th className="py-2 pr-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                  {t('invoices.fields.product')}
                </th>
                <th className="py-2 px-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-600">
                  {t('invoices.fields.quantity')}
                </th>
                <th className="py-2 px-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-600">
                  {t('invoices.fields.unitPrice')}
                </th>
                <th className="py-2 pl-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-600">
                  {t('invoices.fields.lineTotal')}
                </th>
              </tr>
            </thead>
            <tbody>
              {invoice.lines.map((line) => (
                <tr key={line.id} className="border-b border-slate-200">
                  <td className="py-3 pr-2">
                    <div className="font-medium text-slate-900">{line.productName}</div>
                    <div className="font-mono text-[10px] text-slate-500">{line.productSku}</div>
                  </td>
                  <td className="py-3 px-2 text-right text-slate-700">{line.quantity}</td>
                  <td className="py-3 px-2 text-right text-slate-700">
                    {formatCurrency(line.unitPrice, invoice.currency, i18n.language)}
                  </td>
                  <td className="py-3 pl-2 text-right font-medium text-slate-900">
                    {formatCurrency(line.lineTotal, invoice.currency, i18n.language)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td
                  colSpan={3}
                  className="py-3 pr-2 text-right text-sm font-semibold uppercase text-slate-600"
                >
                  {t('invoices.fields.total')}
                </td>
                <td className="py-3 pl-2 text-right text-lg font-bold text-slate-900">
                  {formatCurrency(invoice.total, invoice.currency, i18n.language)}
                </td>
              </tr>
            </tfoot>
          </table>
        </section>

        {invoice.notes && (
          <section className="mt-8 border-t border-slate-200 pt-4">
            <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
              {t('invoices.fields.notes')}
            </div>
            <div className="mt-1 whitespace-pre-line text-sm text-slate-700">{invoice.notes}</div>
          </section>
        )}

        <footer className="mt-12 border-t border-slate-200 pt-4 text-center text-[10px] text-slate-500">
          {t('invoices.print.footer')}
        </footer>
      </div>
    </div>
  );
};

const Row = ({ label, value }: { label: string; value: string }) => (
  <div className="flex justify-end gap-3 text-sm">
    <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
      {label}
    </span>
    <span className="font-medium text-slate-900">{value}</span>
  </div>
);
