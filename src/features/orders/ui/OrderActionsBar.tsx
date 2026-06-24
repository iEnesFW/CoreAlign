import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  ChevronRight,
  Lock,
  PackageCheck,
  Send,
  ShieldCheck,
  Truck,
  XCircle,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useAllocateOrder,
  useApproveOrder,
  useCancelOrder,
  useCloseOrder,
  useDeliverOrder,
  useSubmitOrder,
} from '../hooks/useOrderQueries';
import type { Order, OrderStatus } from '../model/order.types';

interface Props {
  order: Order;
  onShipmentRequested?: () => void;
}

const NEXT_ACTIONS: Record<OrderStatus, string[]> = {
  Draft: ['submit', 'cancel'],
  Submitted: ['approve', 'cancel'],
  Approved: ['allocate', 'cancel'],
  Allocated: ['createShipment', 'cancel'],
  Picking: ['createShipment'],
  Packed: ['createShipment'],
  PartiallyShipped: ['createShipment', 'close'],
  Shipped: ['deliver', 'close'],
  Delivered: ['close'],
  Confirmed: ['createShipment'],
  Closed: [],
  Cancelled: [],
  Returned: [],
};

const ICONS: Record<string, React.ReactNode> = {
  submit: <Send size={14} />,
  approve: <ShieldCheck size={14} />,
  allocate: <PackageCheck size={14} />,
  createShipment: <Truck size={14} />,
  deliver: <CheckCircle2 size={14} />,
  close: <Lock size={14} />,
  cancel: <XCircle size={14} />,
};

export const OrderActionsBar = ({ order, onShipmentRequested }: Props) => {
  const { t } = useTranslation();
  const submitMutation = useSubmitOrder();
  const approveMutation = useApproveOrder();
  const allocateMutation = useAllocateOrder();
  const cancelMutation = useCancelOrder();
  const deliverMutation = useDeliverOrder();
  const closeMutation = useCloseOrder();
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [cancelReason, setCancelReason] = useState('');

  const actions = NEXT_ACTIONS[order.status] ?? [];
  if (actions.length === 0) return null;

  const handleAction = async (action: string) => {
    try {
      if (action === 'submit') {
        await submitMutation.mutateAsync(order.id);
        toast.success(t('orders.actions.submit'));
      } else if (action === 'approve') {
        await approveMutation.mutateAsync({ id: order.id });
        toast.success(t('orders.actions.approve'));
      } else if (action === 'allocate') {
        await allocateMutation.mutateAsync({ id: order.id });
        toast.success(t('orders.actions.allocate'));
      } else if (action === 'createShipment') {
        onShipmentRequested?.();
      } else if (action === 'deliver') {
        await deliverMutation.mutateAsync({ id: order.id });
        toast.success(t('orders.actions.deliver'));
      } else if (action === 'close') {
        await closeMutation.mutateAsync(order.id);
        toast.success(t('orders.actions.close'));
      }
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleCancelConfirm = async () => {
    try {
      await cancelMutation.mutateAsync({ id: order.id, reason: cancelReason || null });
      toast.success(t('orders.actions.cancel'));
      setConfirmCancel(false);
      setCancelReason('');
    } catch (err) {
      toastApiError(err);
    }
  };

  const isBusy =
    submitMutation.isPending ||
    approveMutation.isPending ||
    allocateMutation.isPending ||
    cancelMutation.isPending ||
    deliverMutation.isPending ||
    closeMutation.isPending;

  return (
    <>
      <div className="flex flex-wrap items-center gap-2 rounded-lg border border-slate-200 bg-slate-50/60 p-2 dark:border-slate-800 dark:bg-slate-900/40">
        <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('orders.actions.submit')}
          <ChevronRight className="ml-1 inline" size={11} />
        </span>
        {actions.map((action) => {
          const isCancel = action === 'cancel';
          return (
            <button
              key={action}
              type="button"
              onClick={() => (isCancel ? setConfirmCancel(true) : handleAction(action))}
              disabled={isBusy}
              className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-xs font-medium transition disabled:opacity-50 ${
                isCancel
                  ? 'border-danger-200 bg-white text-danger-700 hover:bg-danger-50 dark:border-danger-500/30 dark:bg-slate-900 dark:text-danger-300 dark:hover:bg-danger-500/10'
                  : 'border-primary-200 bg-white text-primary-700 hover:bg-primary-50 dark:border-primary-500/30 dark:bg-slate-900 dark:text-primary-300 dark:hover:bg-primary-500/10'
              }`}
            >
              {ICONS[action]}
              {t(`orders.actions.${action}` as never)}
            </button>
          );
        })}
      </div>

      {confirmCancel && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={() => setConfirmCancel(false)}
          role="presentation"
        >
          <div
            className="w-full max-w-md rounded-lg bg-white p-5 shadow-xl dark:bg-slate-900"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
              {t('orders.actions.cancel')}
            </h3>
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
              {t('orders.timeline.cancelledAt')} — {order.orderNumber}
            </p>
            <label className="mt-3 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('orders.fields.notes')}
            </label>
            <textarea
              rows={3}
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              className="mt-1 w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800"
              placeholder={t('orders.fields.notesPlaceholder')}
            />
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setConfirmCancel(false)}
                className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                {t('common.cancel')}
              </button>
              <button
                type="button"
                onClick={handleCancelConfirm}
                disabled={cancelMutation.isPending}
                className="inline-flex items-center gap-1.5 rounded bg-danger-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-danger-700 disabled:opacity-50"
              >
                <XCircle size={14} />
                {t('orders.actions.cancel')}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};
