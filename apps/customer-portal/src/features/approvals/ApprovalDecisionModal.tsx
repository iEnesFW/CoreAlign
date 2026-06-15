import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2, XCircle } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Modal } from '@/shared/ui/Modal';
import { Spinner } from '@/shared/ui/Spinner';
import { OrderStatusBadge } from '@/shared/ui/StatusBadge';
import { formatCurrency, formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { portalKeys } from '@/features/portal/hooks';
import { CommentsTab } from '@/features/collaboration/CommentsTab';
import { approvalsApi } from './api';
import { approvalKeys, useApprovalDetail } from './hooks';

interface ApprovalDecisionModalProps {
  orderId: string | null;
  onClose: () => void;
}

export const ApprovalDecisionModal = ({ orderId, onClose }: ApprovalDecisionModalProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const queryClient = useQueryClient();
  const { data, isLoading } = useApprovalDetail(orderId ?? undefined);

  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState('');
  const [trackedOrderId, setTrackedOrderId] = useState(orderId);

  if (orderId !== trackedOrderId) {
    setTrackedOrderId(orderId);
    setRejecting(false);
    setReason('');
  }

  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: approvalKeys.list() }),
      queryClient.invalidateQueries({ queryKey: approvalKeys.pendingCount }),
      queryClient.invalidateQueries({ queryKey: portalKeys.dashboard }),
      queryClient.invalidateQueries({ queryKey: portalKeys.orders() }),
    ]);

  const approveMutation = useMutation({
    mutationFn: () => approvalsApi.approve(orderId!),
    onSuccess: async () => {
      toast.success(t('approvals.approveSuccess'));
      await invalidate();
      onClose();
    },
    onError: (caught: unknown) => {
      const err = caught as { normalizedMessage?: string; message?: string };
      toast.error(err.normalizedMessage ?? err.message ?? t('common.errorGeneric'));
    },
  });

  const rejectMutation = useMutation({
    mutationFn: () => approvalsApi.reject(orderId!, reason.trim()),
    onSuccess: async () => {
      toast.success(t('approvals.rejectSuccess'));
      await invalidate();
      onClose();
    },
    onError: (caught: unknown) => {
      const err = caught as { normalizedMessage?: string; message?: string };
      toast.error(err.normalizedMessage ?? err.message ?? t('common.errorGeneric'));
    },
  });

  if (!orderId) return null;

  const working = approveMutation.isPending || rejectMutation.isPending;

  const onApprove = () => approveMutation.mutate();

  const onReject = () => {
    if (!reason.trim()) {
      toast.error(t('approvals.rejectMissingReason'));
      return;
    }
    rejectMutation.mutate();
  };

  return (
    <Modal
      open
      onClose={onClose}
      size="lg"
      title={t(rejecting ? 'approvals.rejectModalTitle' : 'approvals.modalTitle')}
      description={data ? `${data.orderNumber} • ${data.customerName}` : null}
      footer={
        rejecting ? (
          <>
            <Button variant="ghost" onClick={() => setRejecting(false)} disabled={working}>
              {t('common.back')}
            </Button>
            <Button variant="danger" onClick={onReject} disabled={working}>
              {working ? <Spinner size={14} className="text-white" /> : <XCircle size={14} />}
              {t('approvals.rejectButton')}
            </Button>
          </>
        ) : (
          <>
            <Button variant="ghost" onClick={onClose} disabled={working}>
              {t('common.close')}
            </Button>
            <Button variant="danger" onClick={() => setRejecting(true)} disabled={working || !data}>
              <XCircle size={14} />
              {t('approvals.reject')}
            </Button>
            <Button onClick={onApprove} disabled={working || !data}>
              {working ? <Spinner size={14} className="text-white" /> : <CheckCircle2 size={14} />}
              {t('approvals.approveButton')}
            </Button>
          </>
        )
      }
    >
      {isLoading || !data ? (
        <div className="flex items-center gap-2 text-sm text-slate-500">
          <Spinner /> {t('common.loading')}
        </div>
      ) : rejecting ? (
        <div className="space-y-3">
          <label className="text-sm font-medium text-slate-700 dark:text-slate-200">
            {t('approvals.rejectReasonLabel')}
          </label>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={t('approvals.rejectReasonPlaceholder')}
            className="min-h-[120px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
            autoFocus
          />
        </div>
      ) : (
        <div className="space-y-5">
          <div className="flex flex-wrap items-center gap-3 text-sm">
            <OrderStatusBadge status={data.status} />
            <span className="text-slate-500">
              {data.originDealerName ?? '—'} •{' '}
              {formatDateTime(data.createdAtUtc ?? data.orderDate, locale)}
            </span>
            <span className="ml-auto text-lg font-bold text-slate-900 dark:text-slate-100">
              {formatCurrency(data.total, locale, data.currency)}
            </span>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-100 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th className="px-4 py-2">{t('orders.product')}</th>
                  <th className="px-4 py-2 text-right">{t('orders.quantity')}</th>
                  <th className="px-4 py-2 text-right">{t('orders.unitPrice')}</th>
                  <th className="px-4 py-2 text-right">{t('orders.lineTotal')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
                {data.lines.map((line) => (
                  <tr key={line.id}>
                    <td className="px-4 py-2">
                      <p className="font-medium text-slate-900 dark:text-slate-100">
                        {line.productName}
                      </p>
                      <p className="text-xs text-slate-500">{line.productSku}</p>
                    </td>
                    <td className="px-4 py-2 text-right">{line.quantity}</td>
                    <td className="px-4 py-2 text-right">
                      {formatCurrency(line.unitPrice, locale, data.currency)}
                    </td>
                    <td className="px-4 py-2 text-right font-semibold">
                      {formatCurrency(line.lineTotal, locale, data.currency)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {data.customerNotes ? (
            <p className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-600 dark:bg-slate-800 dark:text-slate-300">
              {data.customerNotes}
            </p>
          ) : null}

          <div className="rounded-xl border border-slate-100 bg-white p-3 dark:border-slate-800 dark:bg-slate-950">
            <CommentsTab orderId={data.id} />
          </div>
        </div>
      )}
    </Modal>
  );
};
