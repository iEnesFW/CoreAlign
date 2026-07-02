import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowRight, Sparkles } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useAllocateOrder,
  useApproveOrder,
  useSubmitOrder,
} from '@/features/orders/hooks/useOrderQueries';
import {
  useAcceptQuote,
  useConvertQuoteToOrder,
  useSendQuote,
} from '@/features/quotes/hooks/useQuoteQueries';
import type { Order } from '@/features/orders/model/order.types';
import type { Quote } from '@/features/quotes/model/quote.types';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import { resolveNextAction } from '../model/nextActionMap';

type Props =
  | {
      entity: 'order';
      order: Order;
      onCreateShipment?: () => void;
      onGenerateInvoice?: () => void;
    }
  | { entity: 'invoice'; invoice: Invoice; onCollectPayment?: () => void }
  | { entity: 'quote'; quote: Quote };

export const NextBestAction = (props: Props) => {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const submitOrder = useSubmitOrder();
  const approveOrder = useApproveOrder();
  const allocateOrder = useAllocateOrder();
  const sendQuote = useSendQuote();
  const acceptQuote = useAcceptQuote();
  const convertQuote = useConvertQuoteToOrder();

  const status =
    props.entity === 'order'
      ? props.order.status
      : props.entity === 'invoice'
        ? props.invoice.status
        : props.quote.status;

  const descriptor = resolveNextAction(props.entity, status);

  if (props.entity === 'quote' && props.quote.convertedOrderId) return null;
  if (!descriptor) return null;
  if (
    props.entity === 'order' &&
    descriptor.action === 'createShipment' &&
    !props.onCreateShipment
  ) {
    return null;
  }
  if (
    props.entity === 'order' &&
    descriptor.action === 'generateInvoice' &&
    !props.onGenerateInvoice
  ) {
    return null;
  }
  if (props.entity === 'invoice' && !props.onCollectPayment) return null;

  const isBusy =
    submitOrder.isPending ||
    approveOrder.isPending ||
    allocateOrder.isPending ||
    sendQuote.isPending ||
    acceptQuote.isPending ||
    convertQuote.isPending;

  const run = async () => {
    try {
      if (props.entity === 'order') {
        switch (descriptor.action) {
          case 'submit':
            await submitOrder.mutateAsync(props.order.id);
            break;
          case 'approve':
            await approveOrder.mutateAsync({ id: props.order.id });
            break;
          case 'allocate':
            await allocateOrder.mutateAsync({ id: props.order.id });
            break;
          case 'createShipment':
            props.onCreateShipment?.();
            return;
          case 'generateInvoice':
            props.onGenerateInvoice?.();
            return;
          default:
            return;
        }
        toast.success(t(descriptor.labelKey, { defaultValue: descriptor.action }));
        return;
      }

      if (props.entity === 'quote') {
        if (descriptor.action === 'send') {
          await sendQuote.mutateAsync(props.quote.id);
        } else if (descriptor.action === 'accept') {
          await acceptQuote.mutateAsync(props.quote.id);
        } else if (descriptor.action === 'convertToOrder') {
          const res = await convertQuote.mutateAsync(props.quote.id);
          if (res.isSuccess && res.data) {
            navigate(`/dashboard/orders?focus=${res.data.id}`);
            return;
          }
        }
        toast.success(t(descriptor.labelKey, { defaultValue: descriptor.action }));
        return;
      }

      props.onCollectPayment?.();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="flex items-center justify-between gap-3 rounded-lg border border-primary-200 bg-primary-50/60 px-3 py-2 dark:border-primary-500/30 dark:bg-primary-500/10">
      <div className="flex items-center gap-2 text-xs text-primary-800 dark:text-primary-200">
        <Sparkles size={14} className="shrink-0" />
        <span>
          <span className="font-semibold">
            {t('NextBestAction.title', { defaultValue: 'Önerilen sonraki adım' })}:
          </span>{' '}
          {t(descriptor.labelKey, { defaultValue: descriptor.action })}
        </span>
      </div>
      <button
        type="button"
        onClick={run}
        disabled={isBusy}
        className="inline-flex shrink-0 items-center gap-1.5 rounded-md bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-primary-700 disabled:opacity-50"
      >
        {t(descriptor.labelKey, { defaultValue: descriptor.action })}
        <ArrowRight size={13} />
      </button>
    </div>
  );
};
