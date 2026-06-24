import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, CheckCircle2, Clock, Download, Send, XCircle } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Modal } from '@/shared/ui/Modal';
import { Spinner } from '@/shared/ui/Spinner';
import { ApprovalStatusBadge, OrderStatusBadge } from '@/shared/ui/StatusBadge';
import { formatCurrency, formatDateTime, formatNumber } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { usePdfDownload } from '@/shared/lib/usePdfDownload';
import { dealerApi } from '@/features/portal/api';
import { dealerKeys, useDealerOrder } from '@/features/portal/hooks';
import { ForwardDocumentModal } from '@/features/portal/ForwardDocumentModal';
import { CommentsTab } from '@/features/collaboration/CommentsTab';

export const OrderDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data, isLoading, isError } = useDealerOrder(id);
  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [forwardOpen, setForwardOpen] = useState(false);
  const pdf = usePdfDownload(
    `/dealer-portal/orders/${id ?? ''}/pdf`,
    `Order-${data?.orderNumber ?? id ?? ''}.pdf`,
  );

  const cancelMutation = useMutation({
    mutationFn: () => dealerApi.cancelOrder(id!, undefined),
    onSuccess: async () => {
      toast.success(t('b2b.orders.cancelled'));
      setCancelConfirmOpen(false);
      if (id) {
        await queryClient.invalidateQueries({ queryKey: dealerKeys.order(id) });
        await queryClient.invalidateQueries({ queryKey: dealerKeys.dashboard });
      }
    },
    onError: (caught: unknown) => {
      const err = caught as { normalizedMessage?: string; message?: string };
      toast.error(err.normalizedMessage ?? err.message ?? t('b2b.common.errorGeneric'));
    },
  });

  const cancelling = cancelMutation.isPending;

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <Spinner /> {t('b2b.common.loading')}
      </div>
    );
  }
  if (isError || !data) {
    return (
      <div className="space-y-3">
        <Button variant="ghost" size="sm" onClick={() => navigate('/orders')}>
          <ArrowLeft size={14} /> {t('b2b.common.back')}
        </Button>
        <p className="text-sm text-slate-500">{t('b2b.common.noData')}</p>
      </div>
    );
  }

  const canCancel = data.dealerApprovalStatus === 'PendingCustomerApproval';

  const openCancelConfirm = () => {
    if (!id) return;
    setCancelConfirmOpen(true);
  };

  const confirmCancel = () => {
    if (!id) return;
    cancelMutation.mutate();
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <Link
          to="/orders"
          className="inline-flex items-center gap-2 text-sm font-medium text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
        >
          <ArrowLeft size={14} /> {t('b2b.orders.title')}
        </Link>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="secondary" size="sm" onClick={pdf.download} disabled={pdf.isLoading}>
            <Download size={14} /> {t('b2b.common.downloadPdf')}
          </Button>
          <Button variant="ghost" size="sm" onClick={() => setForwardOpen(true)}>
            <Send size={14} /> {t('b2b.forward.action')}
          </Button>
          {canCancel ? (
            <Button variant="danger" size="sm" onClick={openCancelConfirm} disabled={cancelling}>
              {cancelling ? <Spinner size={14} className="text-white" /> : null}
              {t('b2b.orders.cancelOrder')}
            </Button>
          ) : (
            <span className="text-xs text-slate-400">{t('b2b.orders.cancelDisabledHint')}</span>
          )}
        </div>
      </div>

      <ForwardDocumentModal
        open={forwardOpen}
        onClose={() => setForwardOpen(false)}
        documentType="Order"
        documentId={id ?? ''}
        documentNumber={data.orderNumber}
      />

      <Modal
        open={cancelConfirmOpen}
        onClose={() => (cancelling ? undefined : setCancelConfirmOpen(false))}
        title={t('b2b.orders.cancelConfirmTitle')}
        size="sm"
        footer={
          <>
            <Button
              type="button"
              variant="ghost"
              onClick={() => setCancelConfirmOpen(false)}
              disabled={cancelling}
            >
              {t('b2b.common.cancel')}
            </Button>
            <Button type="button" variant="danger" onClick={confirmCancel} disabled={cancelling}>
              {cancelling ? <Spinner size={14} className="text-white" /> : null}
              {t('b2b.orders.cancelConfirmAction')}
            </Button>
          </>
        }
      >
        <p className="text-sm text-slate-600 dark:text-slate-300">
          {t('b2b.orders.cancelConfirmBody')}
        </p>
      </Modal>

      <Card>
        <CardHeader
          title={
            <span className="flex flex-wrap items-center gap-3">
              {data.orderNumber}
              <OrderStatusBadge status={data.status} />
              <ApprovalStatusBadge status={data.dealerApprovalStatus} />
            </span>
          }
          subtitle={
            <span className="text-xs text-slate-500">
              {data.customerName} • {formatDateTime(data.orderDate, locale)}
            </span>
          }
        />
        <CardBody>
          <dl className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-3">
            <Field label={t('b2b.orders.customer')} value={data.customerName} />
            <Field
              label={t('b2b.orders.total')}
              value={formatCurrency(data.total, locale, data.currency)}
            />
            <Field label={t('b2b.orders.currency')} value={data.currency} />
          </dl>
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('b2b.orders.timeline')} />
        <CardBody>
          <ol className="space-y-3 text-sm">
            <TimelineItem
              icon={<Clock size={14} className="text-amber-500" />}
              label={t('b2b.orders.submittedByDealer', {
                date: formatDateTime(data.createdAtUtc, locale),
              })}
            />
            {data.dealerApprovalStatus === 'Approved' && data.dealerApprovedAtUtc ? (
              <TimelineItem
                icon={<CheckCircle2 size={14} className="text-emerald-500" />}
                label={t('b2b.orders.approvedByCustomer', {
                  date: formatDateTime(data.dealerApprovedAtUtc, locale),
                })}
              />
            ) : null}
            {data.dealerApprovalStatus === 'Rejected' && data.dealerApprovedAtUtc ? (
              <>
                <TimelineItem
                  icon={<XCircle size={14} className="text-rose-500" />}
                  label={t('b2b.orders.rejectedByCustomer', {
                    date: formatDateTime(data.dealerApprovedAtUtc, locale),
                  })}
                />
                {data.dealerRejectionReason ? (
                  <li className="ml-6 rounded-xl bg-rose-50 px-4 py-2 text-xs text-rose-700 dark:bg-rose-900/30 dark:text-rose-300">
                    {t('b2b.orders.rejectionReason', { reason: data.dealerRejectionReason })}
                  </li>
                ) : null}
              </>
            ) : null}
          </ol>
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('b2b.orders.lines')} />
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
              <tr>
                <th className="px-6 py-3 font-medium">#</th>
                <th className="px-6 py-3 font-medium">{t('b2b.orders.product')}</th>
                <th className="px-6 py-3 text-right font-medium">{t('b2b.orders.quantity')}</th>
                <th className="px-6 py-3 text-right font-medium">{t('b2b.orders.unitPrice')}</th>
                <th className="px-6 py-3 text-right font-medium">{t('b2b.orders.lineTotal')}</th>
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
                    {formatNumber(line.quantity, locale)} {line.uomCode ?? ''}
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
                <td colSpan={4} className="px-6 py-2 text-right text-xs text-slate-500">
                  {t('b2b.orders.subtotal')}
                </td>
                <td className="px-6 py-2 text-right text-slate-700 dark:text-slate-200">
                  {formatCurrency(data.subtotal, locale, data.currency)}
                </td>
              </tr>
              <tr>
                <td colSpan={4} className="px-6 py-2 text-right text-xs text-slate-500">
                  {t('b2b.orders.tax')}
                </td>
                <td className="px-6 py-2 text-right text-slate-700 dark:text-slate-200">
                  {formatCurrency(data.taxTotal, locale, data.currency)}
                </td>
              </tr>
              {data.shippingCost ? (
                <tr>
                  <td colSpan={4} className="px-6 py-2 text-right text-xs text-slate-500">
                    {t('b2b.orders.shipping')}
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
                  {t('b2b.orders.grandTotal')}
                </td>
                <td className="border-t border-slate-200 px-6 py-3 text-right text-base font-bold text-slate-900 dark:border-slate-700 dark:text-slate-100">
                  {formatCurrency(data.total, locale, data.currency)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </Card>

      <Card>
        <CardHeader title={t('b2b.comments.title')} subtitle={t('b2b.comments.subtitle')} />
        <CardBody>
          <CommentsTab orderId={data.id} />
        </CardBody>
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

const TimelineItem = ({ icon, label }: { icon: React.ReactNode; label: string }) => (
  <li className="flex items-start gap-3">
    <span className="mt-0.5">{icon}</span>
    <span className="text-sm text-slate-700 dark:text-slate-300">{label}</span>
  </li>
);
