import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FilePenLine } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
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

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
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
    <Modal
      open={true}
      title={t('orders.revisions.modalTitle')}
      icon={<FilePenLine size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button type="submit" form="request-revision-form" isLoading={requestMutation.isPending}>
            {t('orders.revisions.modalSubmit')}
          </Button>
        </>
      }
    >
      <form id="request-revision-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-2">
          {lines.map((line, idx) => (
            <div
              key={`${line.productId}-${idx}`}
              className="grid grid-cols-12 items-center gap-2 rounded-lg border border-slate-200 px-2 py-1 dark:border-white/10"
            >
              <span className="col-span-6 truncate text-sm">
                <span className="font-medium">{line.productSku}</span>{' '}
                <span className="text-slate-500 dark:text-slate-400">{line.productName}</span>
              </span>
              <input
                type="number"
                min="0"
                step="0.01"
                value={line.quantity}
                onChange={(e) => updateLine(idx, { quantity: Number(e.target.value) })}
                className={`${fieldBaseClasses(false)} col-span-3`}
              />
              <input
                type="number"
                min="0"
                step="0.01"
                value={line.unitPrice}
                onChange={(e) => updateLine(idx, { unitPrice: Number(e.target.value) })}
                className={`${fieldBaseClasses(false)} col-span-3`}
              />
            </div>
          ))}
        </div>

        <Textarea
          label={t('orders.revisions.requestNotes')}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          placeholder={t('orders.revisions.requestNotesPlaceholder')}
        />
      </form>
    </Modal>
  );
}
