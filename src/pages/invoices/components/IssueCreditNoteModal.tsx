import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FileMinus } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCreditNotesForInvoice,
  useIssueCreditNote,
} from '@/features/invoices/hooks/useInvoiceQueries';
import type { Invoice, IssueCreditNoteLineInput } from '@/features/invoices/model/invoice.types';

interface Props {
  invoice: Invoice | null;
  open: boolean;
  onClose: () => void;
  onSuccess?: (creditNoteId: string) => void;
}

interface LineSelection {
  invoiceLineId: string;
  selected: boolean;
  quantity: number;
  maxQuantity: number;
  remaining: number;
}

export const IssueCreditNoteModal = ({ invoice, open, onClose, onSuccess }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const issueMutation = useIssueCreditNote();
  const priorCreditsQuery = useCreditNotesForInvoice(open && invoice ? invoice.id : null);

  const alreadyCreditedByLine = useMemo(() => {
    const map = new Map<string, number>();
    if (!priorCreditsQuery.data?.data) return map;
    return map;
  }, [priorCreditsQuery.data]);

  const lines = useMemo(() => invoice?.lines ?? [], [invoice]);

  const activeKey = open && invoice ? invoice.id : null;
  const initialSelections = useMemo<Record<string, LineSelection>>(() => {
    if (!activeKey) return {};
    const initial: Record<string, LineSelection> = {};
    for (const line of lines) {
      const alreadyCredited = alreadyCreditedByLine.get(line.id) ?? 0;
      const remaining = Math.max(0, line.quantity - alreadyCredited);
      initial[line.id] = {
        invoiceLineId: line.id,
        selected: false,
        quantity: remaining,
        maxQuantity: remaining,
        remaining,
      };
    }
    return initial;
  }, [activeKey, lines, alreadyCreditedByLine]);

  const [trackedKey, setTrackedKey] = useState<string | null>(activeKey);
  const [selections, setSelections] = useState<Record<string, LineSelection>>(initialSelections);
  const [reason, setReason] = useState('');

  if (trackedKey !== activeKey) {
    setTrackedKey(activeKey);
    setSelections(initialSelections);
    setReason('');
  }

  const toggleLine = (id: string, selected: boolean) => {
    setSelections((prev) => ({ ...prev, [id]: { ...prev[id], selected } }));
  };

  const setQuantity = (id: string, value: number) => {
    setSelections((prev) => {
      const current = prev[id];
      if (!current) return prev;
      const clamped = Math.max(0, Math.min(current.maxQuantity, value));
      return { ...prev, [id]: { ...current, quantity: clamped } };
    });
  };

  const chosen = useMemo<IssueCreditNoteLineInput[]>(
    () =>
      Object.values(selections)
        .filter((s) => s.selected && s.quantity > 0)
        .map((s) => ({ invoiceLineId: s.invoiceLineId, quantity: s.quantity })),
    [selections],
  );

  const submit = async () => {
    if (!invoice || chosen.length === 0) return;
    try {
      const response = await issueMutation.mutateAsync({
        id: invoice.id,
        payload: { lines: chosen, reason: reason.trim() || null },
      });
      const newId = response.data?.id;
      toast.success(t('invoices.creditNote.toastSuccess'));
      if (newId && onSuccess) onSuccess(newId);
      onClose();
    } catch (error) {
      toastApiError(error, t('invoices.creditNote.toastError'));
    }
  };

  if (!invoice) return null;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('invoices.creditNote.title')}
      subtitle={t('invoices.creditNote.subtitle', { number: invoice.invoiceNumber })}
      icon={<FileMinus size={16} className="text-rose-500" />}
      size="2xl"
      footer={
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={issueMutation.isPending}>
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button
            variant="primary"
            isLoading={issueMutation.isPending}
            disabled={chosen.length === 0}
            onClick={submit}
          >
            {t('invoices.creditNote.submit')}
          </Button>
        </div>
      }
    >
      <div className="space-y-4 p-4">
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('invoices.creditNote.help')}
        </p>

        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 dark:bg-slate-800/50">
              <tr>
                <th className="w-8 px-2 py-2"></th>
                <th className="px-2 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('invoices.creditNote.product')}
                </th>
                <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('invoices.creditNote.invoiced')}
                </th>
                <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('invoices.creditNote.remaining')}
                </th>
                <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('invoices.creditNote.quantity')}
                </th>
                <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('invoices.creditNote.unitPrice')}
                </th>
                <th className="px-2 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('invoices.creditNote.subtotal')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {lines.map((line) => {
                const sel = selections[line.id];
                if (!sel) return null;
                const subtotal = sel.selected ? sel.quantity * line.unitPrice : 0;
                return (
                  <tr
                    key={line.id}
                    className={sel.selected ? 'bg-rose-50/40 dark:bg-rose-500/5' : ''}
                  >
                    <td className="px-2 py-2">
                      <input
                        type="checkbox"
                        className="h-4 w-4 cursor-pointer accent-rose-500"
                        checked={sel.selected}
                        disabled={sel.remaining <= 0}
                        onChange={(e) => toggleLine(line.id, e.target.checked)}
                        aria-label={t('invoices.creditNote.selectLine', { sku: line.productSku })}
                      />
                    </td>
                    <td className="px-2 py-2">
                      <div className="font-medium text-slate-900 dark:text-slate-100">
                        {line.productName}
                      </div>
                      <div className="font-mono text-[10px] text-slate-500">{line.productSku}</div>
                    </td>
                    <td className="px-2 py-2 text-right tabular-nums text-slate-700 dark:text-slate-300">
                      {line.quantity}
                    </td>
                    <td className="px-2 py-2 text-right tabular-nums text-slate-700 dark:text-slate-300">
                      {sel.remaining}
                    </td>
                    <td className="px-2 py-2 text-right">
                      <input
                        type="number"
                        min={0}
                        max={sel.maxQuantity}
                        step="0.0001"
                        value={sel.quantity}
                        disabled={!sel.selected}
                        onChange={(e) => setQuantity(line.id, Number(e.target.value))}
                        className="w-24 rounded border border-slate-300 px-2 py-1 text-right text-xs tabular-nums focus:border-indigo-500 focus:outline-none disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
                      />
                    </td>
                    <td className="px-2 py-2 text-right tabular-nums text-slate-700 dark:text-slate-300">
                      {formatCurrency(line.unitPrice, locale, invoice.currency)}
                    </td>
                    <td className="px-2 py-2 text-right font-medium tabular-nums text-slate-900 dark:text-slate-100">
                      {formatCurrency(subtotal, locale, invoice.currency)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div>
          <label
            htmlFor="credit-note-reason"
            className="mb-1 block text-xs font-semibold text-slate-700 dark:text-slate-300"
          >
            {t('invoices.creditNote.reason')}
          </label>
          <textarea
            id="credit-note-reason"
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={t('invoices.creditNote.reasonPlaceholder', { defaultValue: '' })}
            className="block w-full rounded border border-slate-300 px-2 py-1.5 text-xs focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-800"
          />
        </div>
      </div>
    </Modal>
  );
};
