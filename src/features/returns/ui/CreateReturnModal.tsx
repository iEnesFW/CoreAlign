import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { RotateCcw } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateReturnRequest } from '../hooks/useReturnQueries';
import { RETURN_REASON_CODES } from '../model/return.types';
import type { CreateReturnRequestLineInput, ReturnReasonCode } from '../model/return.types';
import type { Order } from '@/features/orders/model/order.types';

interface Props {
  order: Order | null;
  open: boolean;
  onClose: () => void;
  onCreated?: (returnId: string) => void;
}

interface ReturnableLine {
  orderLineId: string;
  productName: string;
  productSku: string;
  unitPrice: number;
  returnable: number;
}

export const CreateReturnModal = ({ order, open, onClose, onCreated }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const createMutation = useCreateReturnRequest();

  const returnableLines = useMemo<ReturnableLine[]>(() => {
    if (!order) return [];
    return order.lines
      .map((line) => ({
        orderLineId: line.id,
        productName: line.productName,
        productSku: line.productSku,
        unitPrice: line.unitPrice,
        returnable: line.quantityShipped - line.quantityReturned,
      }))
      .filter((line) => line.returnable > 0);
  }, [order]);

  const activeKey = open && order ? order.id : null;
  const [trackedKey, setTrackedKey] = useState<string | null>(activeKey);
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [reason, setReason] = useState<ReturnReasonCode>('Other');
  const [reasonText, setReasonText] = useState('');
  const [customerNotes, setCustomerNotes] = useState('');

  if (trackedKey !== activeKey) {
    setTrackedKey(activeKey);
    setQuantities({});
    setReason('Other');
    setReasonText('');
    setCustomerNotes('');
  }

  const setQuantity = (orderLineId: string, max: number, value: number) => {
    const clamped = Math.max(0, Math.min(max, Number.isFinite(value) ? value : 0));
    setQuantities((prev) => ({ ...prev, [orderLineId]: clamped }));
  };

  const chosen = useMemo<CreateReturnRequestLineInput[]>(
    () =>
      returnableLines
        .map((line) => ({
          orderLineId: line.orderLineId,
          quantityReturned: quantities[line.orderLineId] ?? 0,
        }))
        .filter((line) => line.quantityReturned > 0),
    [returnableLines, quantities],
  );

  const hasReturnableLines = returnableLines.length > 0;

  const submit = async () => {
    if (!order) return;
    if (chosen.length === 0) {
      toast.error(t('Returns.create.atLeastOneLine'));
      return;
    }
    try {
      const response = await createMutation.mutateAsync({
        orderId: order.id,
        reason,
        reasonText: reasonText.trim() || undefined,
        customerNotes: customerNotes.trim() || undefined,
        lines: chosen,
      });
      const newId = response.data?.id;
      toast.success(t('Returns.create.success'));
      if (newId && onCreated) onCreated(newId);
      onClose();
    } catch (error) {
      toastApiError(error);
    }
  };

  if (!order) return null;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('Returns.create.title')}
      subtitle={t('Returns.create.subtitle', { number: order.orderNumber })}
      icon={<RotateCcw size={16} />}
      size="2xl"
      footer={
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={createMutation.isPending}>
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button
            variant="primary"
            isLoading={createMutation.isPending}
            disabled={!hasReturnableLines || chosen.length === 0}
            onClick={submit}
          >
            {t('Returns.create.submit')}
          </Button>
        </div>
      }
    >
      <div className="space-y-4 p-4">
        {!hasReturnableLines ? (
          <div className="rounded-lg border border-slate-200 py-10 text-center text-xs text-slate-500 dark:border-slate-800 dark:text-slate-400">
            {t('Returns.create.noReturnableLines')}
          </div>
        ) : (
          <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 dark:bg-slate-800/50">
                <tr>
                  <th className="px-2 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    {t('Returns.fields.product')}
                  </th>
                  <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    {t('Returns.fields.unitPrice')}
                  </th>
                  <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    {t('Returns.create.quantityReturned')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                {returnableLines.map((line) => (
                  <tr key={line.orderLineId}>
                    <td className="px-2 py-2">
                      <div className="font-medium text-slate-900 dark:text-slate-100">
                        {line.productName}
                      </div>
                      <div className="font-mono text-[10px] text-slate-500 dark:text-slate-400">
                        {line.productSku}
                      </div>
                      <div className="mt-0.5 text-[10px] text-slate-500 dark:text-slate-400">
                        {t('Returns.create.returnableQty', {
                          qty: formatNumber(line.returnable, locale, 0),
                        })}
                      </div>
                    </td>
                    <td className="px-2 py-2 text-right tabular-nums text-slate-700 dark:text-slate-300">
                      {formatCurrency(line.unitPrice, locale, order.currency)}
                    </td>
                    <td className="px-2 py-2 text-right">
                      <input
                        type="number"
                        min={0}
                        max={line.returnable}
                        step={1}
                        value={quantities[line.orderLineId] ?? 0}
                        aria-label={t('Returns.create.quantityReturned')}
                        onChange={(e) =>
                          setQuantity(line.orderLineId, line.returnable, Number(e.target.value))
                        }
                        className="w-24 rounded border border-slate-300 px-2 py-1 text-right text-xs tabular-nums focus:border-primary-500 focus:outline-none dark:border-slate-700 dark:bg-slate-800"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div>
          <label
            htmlFor="create-return-reason"
            className="mb-1 block text-xs font-semibold text-slate-700 dark:text-slate-300"
          >
            {t('Returns.create.reason')}
          </label>
          <select
            id="create-return-reason"
            value={reason}
            onChange={(e) => setReason(e.target.value as ReturnReasonCode)}
            disabled={!hasReturnableLines}
            className="block w-full rounded border border-slate-300 px-2 py-1.5 text-xs focus:border-primary-500 focus:outline-none disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
          >
            {RETURN_REASON_CODES.map((code) => (
              <option key={code} value={code}>
                {t(`Returns.reason.${code}` as const)}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label
            htmlFor="create-return-reason-text"
            className="mb-1 block text-xs font-semibold text-slate-700 dark:text-slate-300"
          >
            {t('Returns.create.reasonText')}
          </label>
          <input
            id="create-return-reason-text"
            type="text"
            value={reasonText}
            onChange={(e) => setReasonText(e.target.value)}
            disabled={!hasReturnableLines}
            className="block w-full rounded border border-slate-300 px-2 py-1.5 text-xs focus:border-primary-500 focus:outline-none disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
          />
        </div>

        <div>
          <label
            htmlFor="create-return-customer-notes"
            className="mb-1 block text-xs font-semibold text-slate-700 dark:text-slate-300"
          >
            {t('Returns.create.customerNotes')}
          </label>
          <textarea
            id="create-return-customer-notes"
            rows={3}
            value={customerNotes}
            onChange={(e) => setCustomerNotes(e.target.value)}
            disabled={!hasReturnableLines}
            className="block w-full rounded border border-slate-300 px-2 py-1.5 text-xs focus:border-primary-500 focus:outline-none disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
          />
        </div>
      </div>
    </Modal>
  );
};
