import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import type { Order } from '../model/order.types';
import {
  useApproveOrderRevision,
  useCancelOrderRevision,
  useOrderRevisionsQuery,
  useRejectOrderRevision,
} from '../hooks/useOrderRevisionQueries';
import type { OrderRevision } from '../api/orderRevisionsApi';
import { RequestRevisionModal } from './RequestRevisionModal';

interface Props {
  order: Order;
  currentUserId?: string | null;
}

export function OrderRevisionsTab({ order, currentUserId }: Props) {
  const { t, i18n } = useTranslation();
  const [requestOpen, setRequestOpen] = useState(false);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const revisionsQuery = useOrderRevisionsQuery(order.id);
  const approveMutation = useApproveOrderRevision(order.id);
  const rejectMutation = useRejectOrderRevision(order.id);
  const cancelMutation = useCancelOrderRevision(order.id);

  const timeline = revisionsQuery.data;
  const revisions = timeline?.revisions ?? [];

  const canRequest = ['Submitted', 'Approved', 'Allocated', 'Picking'].includes(order.status);

  const formatDate = (iso: string) => new Date(iso).toLocaleString(i18n.language || 'en');

  const handleApprove = (rev: OrderRevision) => {
    approveMutation.mutate(rev.id, {
      onSuccess: () => toast.success(t('orders.revisions.approveButton')),
      onError: (err) => toastApiError(err),
    });
  };

  const handleReject = (rev: OrderRevision) => {
    if (!rejectReason.trim()) return;
    rejectMutation.mutate(
      { revisionId: rev.id, reason: rejectReason.trim() },
      {
        onSuccess: () => {
          toast.success(t('orders.revisions.rejectButton'));
          setRejectingId(null);
          setRejectReason('');
        },
        onError: (err) => toastApiError(err),
      },
    );
  };

  const handleCancel = (rev: OrderRevision) => {
    cancelMutation.mutate(rev.id, {
      onSuccess: () => toast.success(t('orders.revisions.cancelButton')),
      onError: (err) => toastApiError(err),
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-base font-semibold">{t('orders.revisions.tabTitle')}</h3>
          <p className="text-sm text-muted-foreground">
            {t('orders.revisions.appliedCount', { count: timeline?.appliedRevisionCount ?? 0 })}
          </p>
        </div>
        {canRequest && (
          <button
            type="button"
            onClick={() => setRequestOpen(true)}
            className="rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            {t('orders.revisions.requestButton')}
          </button>
        )}
      </div>

      {revisionsQuery.isLoading && <p className="text-sm text-muted-foreground">…</p>}

      {!revisionsQuery.isLoading && revisions.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('orders.revisions.empty')}</p>
      )}

      <ol className="space-y-3">
        {revisions.map((rev) => {
          const isPending = rev.status === 'Proposed';
          const isRequester = currentUserId && rev.requestedByUserId === currentUserId;
          const personaLabel = t(`orders.revisions.persona.${rev.requestedByPersona}`, {
            defaultValue: rev.requestedByPersona,
          });
          return (
            <li
              key={rev.id}
              className={`rounded-md border p-4 ${
                isPending
                  ? 'border-warning-300 bg-warning-50 dark:border-warning-700 dark:bg-warning-900/20'
                  : 'border-border'
              }`}
            >
              <div className="flex items-start justify-between gap-4">
                <div>
                  <h4 className="font-medium">
                    {t('orders.revisions.revisionNumber', { number: rev.revisionNumber })}{' '}
                    <span className="ml-2 inline-block rounded bg-secondary px-2 py-0.5 text-xs">
                      {t(`orders.revisions.status.${rev.status}`)}
                    </span>
                  </h4>
                  <p className="text-xs text-muted-foreground">
                    {t('orders.revisions.requestedAt', {
                      when: formatDate(rev.requestedAtUtc),
                      persona: personaLabel,
                    })}
                  </p>
                  {rev.decidedAtUtc && (
                    <p className="text-xs text-muted-foreground">
                      {t('orders.revisions.decidedAt', { when: formatDate(rev.decidedAtUtc) })}
                    </p>
                  )}
                  {rev.rejectionReason && (
                    <p className="mt-1 text-xs text-danger-600">
                      {t('orders.revisions.rejectionReason')}: {rev.rejectionReason}
                    </p>
                  )}
                  {rev.requestNotes && (
                    <p className="mt-1 text-xs italic text-muted-foreground">{rev.requestNotes}</p>
                  )}
                </div>
                {isPending && (
                  <div className="flex gap-2">
                    {!isRequester && (
                      <>
                        <button
                          type="button"
                          onClick={() => handleApprove(rev)}
                          disabled={approveMutation.isPending}
                          className="rounded-md bg-success-600 px-3 py-1 text-xs text-white hover:bg-success-500"
                        >
                          {t('orders.revisions.approveButton')}
                        </button>
                        <button
                          type="button"
                          onClick={() => setRejectingId(rev.id)}
                          className="rounded-md bg-danger-600 px-3 py-1 text-xs text-white hover:bg-danger-500"
                        >
                          {t('orders.revisions.rejectButton')}
                        </button>
                      </>
                    )}
                    {isRequester && (
                      <button
                        type="button"
                        onClick={() => handleCancel(rev)}
                        disabled={cancelMutation.isPending}
                        className="rounded-md border px-3 py-1 text-xs"
                      >
                        {t('orders.revisions.cancelButton')}
                      </button>
                    )}
                  </div>
                )}
              </div>

              <details className="mt-3">
                <summary className="cursor-pointer text-xs text-muted-foreground">
                  {t('orders.revisions.proposedLines')}
                </summary>
                <table className="mt-2 w-full text-xs">
                  <tbody>
                    {rev.proposedLines.map((line) => (
                      <tr key={`${rev.id}-${line.productId}-${line.lineNumber}`}>
                        <td className="py-1 pr-2">{line.productSku}</td>
                        <td className="py-1 pr-2">{line.productName}</td>
                        <td className="py-1 pr-2 text-right">{line.quantity}</td>
                        <td className="py-1 text-right">{line.unitPrice}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </details>

              {rejectingId === rev.id && (
                <div className="mt-3 space-y-2 border-t pt-3">
                  <label className="text-xs font-medium">
                    {t('orders.revisions.rejectionReason')}
                  </label>
                  <textarea
                    value={rejectReason}
                    onChange={(e) => setRejectReason(e.target.value)}
                    rows={2}
                    className="w-full rounded-md border px-2 py-1 text-sm"
                    placeholder={t('orders.revisions.rejectionReasonPlaceholder')}
                  />
                  <div className="flex justify-end gap-2">
                    <button
                      type="button"
                      onClick={() => {
                        setRejectingId(null);
                        setRejectReason('');
                      }}
                      className="rounded-md border px-3 py-1 text-xs"
                    >
                      {t('common.cancel', { defaultValue: 'Cancel' })}
                    </button>
                    <button
                      type="button"
                      onClick={() => handleReject(rev)}
                      disabled={rejectMutation.isPending || !rejectReason.trim()}
                      className="rounded-md bg-danger-600 px-3 py-1 text-xs text-white hover:bg-danger-500 disabled:opacity-50"
                    >
                      {t('orders.revisions.rejectButton')}
                    </button>
                  </div>
                </div>
              )}
            </li>
          );
        })}
      </ol>

      {requestOpen && <RequestRevisionModal order={order} onClose={() => setRequestOpen(false)} />}
    </div>
  );
}
