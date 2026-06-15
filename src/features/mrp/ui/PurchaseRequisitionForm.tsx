import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Trash2, Plus } from 'lucide-react';
import type {
  CreatePurchaseRequisitionInput,
  PurchaseRequisitionLineInput,
  PurchaseRequisitionReason,
} from '../model/mrp.types';

interface Props {
  initialValues?: Partial<CreatePurchaseRequisitionInput>;
  onSubmit: (input: CreatePurchaseRequisitionInput) => Promise<void> | void;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

const REASON_OPTIONS: PurchaseRequisitionReason[] = [
  'Manual',
  'EmergencyOrder',
  'StockOut',
  'MRPSuggestion',
];

const emptyLine = (): PurchaseRequisitionLineInput => ({
  productId: '',
  quantityRequested: 0,
  estimatedUnitCost: 0,
  preferredSupplierId: null,
  expectedDeliveryDate: null,
  notes: null,
});

export const PurchaseRequisitionForm = ({
  initialValues,
  onSubmit,
  onCancel,
  isSubmitting = false,
}: Props) => {
  const { t } = useTranslation();
  const [reason, setReason] = useState<PurchaseRequisitionReason>(
    initialValues?.reason ?? 'Manual',
  );
  const [notes, setNotes] = useState<string>(initialValues?.notes ?? '');
  const [lines, setLines] = useState<PurchaseRequisitionLineInput[]>(
    initialValues?.lines && initialValues.lines.length > 0 ? initialValues.lines : [emptyLine()],
  );

  const updateLine = <K extends keyof PurchaseRequisitionLineInput>(
    index: number,
    field: K,
    value: PurchaseRequisitionLineInput[K],
  ) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, [field]: value } : l)));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const cleaned = lines.filter((l) => l.productId && l.quantityRequested > 0);
    if (cleaned.length === 0) return;
    await onSubmit({ reason, notes: notes || null, lines: cleaned });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('Mrp.Requisition.Reason.Label')}
          </span>
          <select
            value={reason}
            onChange={(e) => setReason(e.target.value as PurchaseRequisitionReason)}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          >
            {REASON_OPTIONS.map((r) => (
              <option key={r} value={r}>
                {t(`Mrp.Requisition.Reason.${r}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('Mrp.Requisition.Notes')}
          </span>
          <input
            type="text"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('Mrp.Requisition.Lines')}
          </h3>
          <button
            type="button"
            onClick={() => setLines((prev) => [...prev, emptyLine()])}
            className="flex items-center gap-1 rounded-md border border-indigo-300 bg-indigo-50 px-2 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-100 dark:border-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300"
          >
            <Plus className="h-3.5 w-3.5" />
            {t('Mrp.Requisition.AddLine')}
          </button>
        </div>

        {lines.map((line, idx) => (
          <div
            key={idx}
            className="grid grid-cols-2 gap-2 rounded-md border border-slate-200 bg-slate-50 p-2 dark:border-slate-700 dark:bg-slate-800/50 sm:grid-cols-5"
          >
            <input
              type="text"
              placeholder={t('Mrp.Requisition.ProductId') ?? ''}
              value={line.productId}
              onChange={(e) => updateLine(idx, 'productId', e.target.value)}
              className="col-span-2 rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
            <input
              type="number"
              min={0}
              step="0.01"
              placeholder={t('Mrp.Requisition.Quantity') ?? ''}
              value={line.quantityRequested}
              onChange={(e) => updateLine(idx, 'quantityRequested', Number(e.target.value))}
              className="rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
            <input
              type="number"
              min={0}
              step="0.01"
              placeholder={t('Mrp.Requisition.UnitCost') ?? ''}
              value={line.estimatedUnitCost}
              onChange={(e) => updateLine(idx, 'estimatedUnitCost', Number(e.target.value))}
              className="rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
            <button
              type="button"
              onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
              className="flex items-center justify-center rounded border border-rose-300 bg-rose-50 text-rose-600 hover:bg-rose-100 dark:border-rose-700 dark:bg-rose-500/10"
              aria-label={t('Mrp.Requisition.RemoveLine') ?? 'Remove'}
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        ))}
      </div>

      <div className="flex justify-end gap-2">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            {t('Common.Cancel')}
          </button>
        )}
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-indigo-400"
        >
          {t('Common.Save')}
        </button>
      </div>
    </form>
  );
};
