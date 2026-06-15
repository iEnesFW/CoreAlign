import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import type { Order } from '../model/order.types';
import { useRequestOrderRevision } from '../hooks/useOrderRevisionQueries';
import type { RevisionLineInput } from '../api/orderRevisionsApi';

interface Props {
  order: Order;
  onClose: () => void;
}

interface DraftLine extends RevisionLineInput {
  productSku: string;
  productName: string;
}

export function RequestRevisionModal({ order, onClose }: Props) {
  const { t } = useTranslation();
  const requestMutation = useRequestOrderRevision(order.id);

  const initialLines = useMemo<DraftLine[]>(
    () =>
      order.lines.map((l) => ({
        productId: l.productId,
        productSku: l.productSku,
        productName: l.productName,
        lineNumber: l.lineNumber,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        lineDiscountPercent: l.lineDiscountPercent,
        lineDiscountAmount: l.lineDiscountAmount,
        taxRatePercent: l.taxRatePercent,
        isTaxInclusive: l.isTaxInclusive,
        withholdingRatePercent: l.withholdingRatePercent,
        lineNotes: l.lineNotes,
      })),
    [order],
  );

  const [lines, setLines] = useState<DraftLine[]>(initialLines);
  const [notes, setNotes] = useState('');

  const updateLine = (idx: number, patch: Partial<DraftLine>) =>
    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, ...patch } : l)));

  const handleSubmit = () => {
    const valid = lines.filter((l) => l.quantity > 0);
    if (valid.length === 0) return;
    requestMutation.mutate(
      { proposedLines: valid, requestNotes: notes || null },
      {
        onSuccess: () => {
          toast.success(t('orders.revisions.requestButton'));
          onClose();
        },
        onError: (err) => toastApiError(err),
      },
    );
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-2xl rounded-lg bg-background p-6 shadow-xl">
        <h2 className="text-lg font-semibold">{t('orders.revisions.modalTitle')}</h2>

        <div className="mt-4 space-y-2">
          {lines.map((line, idx) => (
            <div
              key={`${line.productId}-${idx}`}
              className="grid grid-cols-12 items-center gap-2 rounded border px-2 py-1"
            >
              <span className="col-span-6 truncate text-sm">
                <span className="font-medium">{line.productSku}</span>{' '}
                <span className="text-muted-foreground">{line.productName}</span>
              </span>
              <input
                type="number"
                min="0"
                step="0.01"
                value={line.quantity}
                onChange={(e) => updateLine(idx, { quantity: Number(e.target.value) })}
                className="col-span-3 rounded border px-2 py-1 text-sm"
              />
              <input
                type="number"
                min="0"
                step="0.01"
                value={line.unitPrice}
                onChange={(e) => updateLine(idx, { unitPrice: Number(e.target.value) })}
                className="col-span-3 rounded border px-2 py-1 text-sm"
              />
            </div>
          ))}
        </div>

        <div className="mt-4">
          <label className="text-sm font-medium">{t('orders.revisions.requestNotes')}</label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={3}
            placeholder={t('orders.revisions.requestNotesPlaceholder')}
            className="mt-1 w-full rounded-md border px-2 py-1 text-sm"
          />
        </div>

        <div className="mt-6 flex justify-end gap-2">
          <button type="button" onClick={onClose} className="rounded-md border px-3 py-2 text-sm">
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={requestMutation.isPending}
            className="rounded-md bg-primary px-3 py-2 text-sm text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {t('orders.revisions.modalSubmit')}
          </button>
        </div>
      </div>
    </div>
  );
}
