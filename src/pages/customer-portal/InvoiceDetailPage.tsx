import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, CreditCard, Download } from 'lucide-react';
import {
  downloadInvoicePdf,
  useMyInvoiceQuery,
} from '@/features/customer-portal/hooks/useCustomerPortalQueries';

const triggerBrowserDownload = (blob: Blob, filename: string) => {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
};

export const InvoiceDetailPage = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useMyInvoiceQuery(id);

  const invoice = data?.data;

  const onDownload = async () => {
    if (!invoice) return;
    const response = await downloadInvoicePdf(invoice.id);
    triggerBrowserDownload(response.data, `${invoice.invoiceNumber}.pdf`);
  };

  return (
    <div className="space-y-4">
      <Link
        to="/customer-portal/invoices"
        className="inline-flex items-center gap-1 text-sm text-primary-600 hover:underline"
      >
        <ChevronLeft size={16} /> {t('CustomerPortal.Common.Back')}
      </Link>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError || !invoice ? (
        <div className="text-sm text-danger-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : (
        <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
            <div>
              <h1 className="text-xl font-semibold">{invoice.invoiceNumber}</h1>
              <p className="text-xs text-slate-500 mt-0.5">
                {t(`CustomerPortal.Invoice.Status.${invoice.status}`)}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={onDownload}
                className="inline-flex items-center gap-1.5 px-3 py-2 rounded-md text-sm border border-slate-300 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800"
              >
                <Download size={16} /> {t('CustomerPortal.Invoice.Download')}
              </button>
              {(invoice.status === 'Issued' ||
                invoice.status === 'Sent' ||
                invoice.status === 'PartiallyPaid' ||
                invoice.status === 'Overdue') && (
                <Link
                  to={`/customer-portal/payments/initiate?invoiceId=${invoice.id}`}
                  className="inline-flex items-center gap-1.5 px-3 py-2 rounded-md text-sm bg-primary-600 text-white hover:bg-primary-700"
                >
                  <CreditCard size={16} /> {t('CustomerPortal.Invoice.PayNow')}
                </Link>
              )}
            </div>
          </div>
          <dl className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Invoice.Total')}</dt>
              <dd className="font-medium">
                {invoice.total.toLocaleString(undefined, { maximumFractionDigits: 2 })}{' '}
                {invoice.currency}
              </dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Invoice.Paid')}</dt>
              <dd>
                {invoice.amountPaid.toLocaleString(undefined, { maximumFractionDigits: 2 })}{' '}
                {invoice.currency}
              </dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Invoice.Due')}</dt>
              <dd>
                {invoice.amountDue.toLocaleString(undefined, { maximumFractionDigits: 2 })}{' '}
                {invoice.currency}
              </dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Invoice.IssueDate')}</dt>
              <dd>{new Date(invoice.issueDate).toLocaleDateString()}</dd>
            </div>
          </dl>
        </div>
      )}
    </div>
  );
};

export default InvoiceDetailPage;
