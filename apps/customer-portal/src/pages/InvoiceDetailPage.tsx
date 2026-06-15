import { ArrowLeft, CreditCard, Download } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Spinner } from '@/shared/ui/Spinner';
import { InvoiceStatusBadge } from '@/shared/ui/StatusBadge';
import { formatCurrency, formatDate, formatNumber } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { usePdfDownload } from '@/shared/lib/usePdfDownload';
import { usePortalInvoice } from '@/features/portal/hooks';
import { usePayInvoice } from '@/features/portal/profileHooks';

const PAYABLE_STATUSES = new Set(['Issued', 'Sent', 'PartiallyPaid', 'Overdue']);

export const InvoiceDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const { data, isLoading, isError } = usePortalInvoice(id);
  const pdf = usePdfDownload(
    `/customer-portal/invoices/${id ?? ''}/pdf`,
    `Invoice-${data?.invoiceNumber ?? id ?? ''}.pdf`,
  );
  const payInvoice = usePayInvoice();

  const canPay = !!data && PAYABLE_STATUSES.has(data.status) && data.amountDue > 0;

  const onPay = async () => {
    if (!id || !canPay) return;
    try {
      const result = await payInvoice.mutateAsync(id);
      if (result.redirectUrl) {
        toast.success(t('invoices.payStarted'));
        window.location.href = result.redirectUrl;
      } else {
        toast.error(t('common.errorGeneric'));
      }
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <Spinner /> {t('common.loading')}
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="space-y-3">
        <Button variant="ghost" size="sm" onClick={() => navigate('/invoices')}>
          <ArrowLeft size={14} /> {t('common.back')}
        </Button>
        <p className="text-sm text-slate-500">{t('common.noData')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-2">
        <Link
          to="/invoices"
          className="inline-flex items-center gap-2 text-sm font-medium text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
        >
          <ArrowLeft size={14} /> {t('invoices.title')}
        </Link>
        <div className="flex items-center gap-2">
          {canPay && (
            <Button variant="primary" size="sm" onClick={onPay} disabled={payInvoice.isPending}>
              <CreditCard size={14} />{' '}
              {payInvoice.isPending ? t('invoices.paying') : t('invoices.payNow')}
            </Button>
          )}
          <Button variant="secondary" size="sm" onClick={pdf.download} disabled={pdf.isLoading}>
            <Download size={14} /> {t('common.downloadPdf')}
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader
          title={
            <span className="flex items-center gap-3">
              {data.invoiceNumber}
              <InvoiceStatusBadge status={data.status} isOverdue={data.isOverdue} />
            </span>
          }
          subtitle={
            <span className="text-xs text-slate-500">
              {formatDate(data.issueDate, locale)} • {data.customerName}
            </span>
          }
        />
        <CardBody>
          <dl className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-4">
            <Field label={t('invoices.issueDate')} value={formatDate(data.issueDate, locale)} />
            <Field label={t('invoices.dueDate')} value={formatDate(data.dueDate, locale)} />
            <Field
              label={t('invoices.amountPaid')}
              value={formatCurrency(data.amountPaid, locale, data.currency)}
            />
            <Field
              label={t('invoices.amountDue')}
              value={formatCurrency(data.amountDue, locale, data.currency)}
            />
          </dl>
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('invoices.lines')} />
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
              <tr>
                <th scope="col" className="px-6 py-3 font-medium">
                  #
                </th>
                <th scope="col" className="px-6 py-3 font-medium">
                  {t('orders.product')}
                </th>
                <th scope="col" className="px-6 py-3 text-right font-medium">
                  {t('orders.quantity')}
                </th>
                <th scope="col" className="px-6 py-3 text-right font-medium">
                  {t('orders.unitPrice')}
                </th>
                <th scope="col" className="px-6 py-3 text-right font-medium">
                  {t('orders.lineTotal')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
              {data.lines.map((line) => (
                <tr key={line.id}>
                  <td className="px-6 py-3 text-slate-500">{line.lineNumber}</td>
                  <td className="px-6 py-3">
                    <p className="font-medium text-slate-900 dark:text-slate-100">
                      {line.productName}
                    </p>
                    <p className="text-xs text-slate-500">{line.productSku}</p>
                  </td>
                  <td className="px-6 py-3 text-right text-slate-700 dark:text-slate-200">
                    {formatNumber(line.quantity, locale)}
                  </td>
                  <td className="px-6 py-3 text-right text-slate-700 dark:text-slate-200">
                    {formatCurrency(line.unitPrice, locale, data.currency)}
                  </td>
                  <td className="px-6 py-3 text-right font-semibold text-slate-900 dark:text-slate-100">
                    {formatCurrency(line.lineTotal, locale, data.currency)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="bg-slate-50 dark:bg-slate-900">
              <tr>
                <td
                  colSpan={4}
                  className="px-6 py-3 text-right text-xs uppercase tracking-wide text-slate-500"
                >
                  {t('orders.subtotal')}
                </td>
                <td className="px-6 py-3 text-right text-slate-700 dark:text-slate-200">
                  {formatCurrency(data.subtotal, locale, data.currency)}
                </td>
              </tr>
              <tr>
                <td
                  colSpan={4}
                  className="px-6 py-2 text-right text-xs uppercase tracking-wide text-slate-500"
                >
                  {t('orders.tax')}
                </td>
                <td className="px-6 py-2 text-right text-slate-700 dark:text-slate-200">
                  {formatCurrency(data.taxTotal, locale, data.currency)}
                </td>
              </tr>
              {data.shippingCost ? (
                <tr>
                  <td
                    colSpan={4}
                    className="px-6 py-2 text-right text-xs uppercase tracking-wide text-slate-500"
                  >
                    {t('orders.shipping')}
                  </td>
                  <td className="px-6 py-2 text-right text-slate-700 dark:text-slate-200">
                    {formatCurrency(data.shippingCost, locale, data.currency)}
                  </td>
                </tr>
              ) : null}
              <tr>
                <td
                  colSpan={4}
                  className="border-t border-slate-200 px-6 py-3 text-right text-sm font-semibold text-slate-900 dark:border-slate-700 dark:text-slate-100"
                >
                  {t('orders.grandTotal')}
                </td>
                <td className="border-t border-slate-200 px-6 py-3 text-right text-base font-bold text-slate-900 dark:border-slate-700 dark:text-slate-100">
                  {formatCurrency(data.total, locale, data.currency)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </Card>
    </div>
  );
};

const Field = ({ label, value }: { label: string; value: React.ReactNode }) => (
  <div>
    <dt className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</dt>
    <dd className="mt-1 text-sm font-medium text-slate-900 dark:text-slate-100">{value}</dd>
  </div>
);
