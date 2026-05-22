import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Boxes, Info, Minus, Plus, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import {
  useAdjustStock,
  useLotsByProductQuery,
  useReasonCodesQuery,
} from '../hooks/useInventoryQueries';

interface Props {
  open: boolean;
  productId: string;
  productSku: string;
  productName: string;
  currency: string;
  presetWarehouseId?: string | null;
  presetLotId?: string | null;
  currentOnHand?: number | null;
  currentAvgCost?: number | null;
  onClose: () => void;
}

type AdjustmentMode = 'add' | 'remove';

export const AdjustStockModal = ({
  open,
  productId,
  productSku,
  productName,
  currency,
  presetWarehouseId,
  presetLotId,
  currentOnHand,
  currentAvgCost,
  onClose,
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const warehousesQuery = useWarehousesQuery(true);
  const lotsQuery = useLotsByProductQuery(productId);
  const reasonsQuery = useReasonCodesQuery('Adjustment', true);
  const adjustMutation = useAdjustStock();

  const [mode, setMode] = useState<AdjustmentMode>('add');
  const [warehouseId, setWarehouseId] = useState(presetWarehouseId ?? '');
  const [lotId, setLotId] = useState(presetLotId ?? '');
  const [quantity, setQuantity] = useState('1');
  const [unitCost, setUnitCost] = useState(
    currentAvgCost !== null && currentAvgCost !== undefined ? String(currentAvgCost) : '',
  );
  const [reasonCodeId, setReasonCodeId] = useState('');
  const [notes, setNotes] = useState('');

  const warehouses = warehousesQuery.data?.data ?? [];
  const lots = lotsQuery.data?.data ?? [];
  const reasons = reasonsQuery.data?.data ?? [];

  const parsedQty = Number(quantity);
  const parsedUnitCost = unitCost === '' ? null : Number(unitCost);
  const delta = useMemo(() => {
    if (!Number.isFinite(parsedQty) || parsedQty <= 0) return 0;
    return mode === 'add' ? parsedQty : -parsedQty;
  }, [mode, parsedQty]);

  const projectedOnHand =
    currentOnHand !== null && currentOnHand !== undefined ? currentOnHand + delta : null;
  const wouldGoNegative = projectedOnHand !== null && projectedOnHand < 0;

  const canSubmit =
    warehouseId !== '' && Number.isFinite(parsedQty) && parsedQty > 0 && !wouldGoNegative;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    try {
      await adjustMutation.mutateAsync({
        productId,
        warehouseId,
        delta,
        unitCost:
          mode === 'add' && parsedUnitCost !== null && parsedUnitCost >= 0 ? parsedUnitCost : null,
        reasonCodeId: reasonCodeId || null,
        lotId: lotId || null,
        notes: notes || null,
      });
      toast.success(t('inventory.adjust.success'));
      onClose();
    } catch (err) {
      toastApiError(err, t('inventory.adjust.errorFallback'));
    }
  };

  const [dirty, setDirty] = useState(false);
  const requestClose = useModalClose(dirty, onClose, open);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={requestClose}
      role="presentation"
    >
      <div
        className="w-full max-w-xl overflow-hidden rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        onChange={() => setDirty(true)}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-3 dark:border-slate-800">
          <div>
            <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
              {t('inventory.adjust.title')}
            </h2>
            <p className="text-[11px] text-slate-500 dark:text-slate-400">
              {productSku} · {productName}
            </p>
          </div>
          <button
            type="button"
            onClick={requestClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.cancel')}
          >
            <X size={18} />
          </button>
        </div>

        <div className="space-y-4 px-5 py-4">
          <div className="flex gap-1 rounded-lg border border-slate-200 bg-slate-50 p-1 dark:border-slate-700 dark:bg-slate-800/40">
            <button
              type="button"
              onClick={() => setMode('add')}
              className={`flex-1 inline-flex items-center justify-center gap-1.5 rounded px-3 py-1.5 text-sm font-medium transition ${
                mode === 'add'
                  ? 'bg-white text-emerald-700 shadow-sm dark:bg-slate-900 dark:text-emerald-300'
                  : 'text-slate-600 hover:bg-white/60 dark:text-slate-300'
              }`}
            >
              <Plus size={14} />
              {t('inventory.adjust.modeAdd')}
            </button>
            <button
              type="button"
              onClick={() => setMode('remove')}
              className={`flex-1 inline-flex items-center justify-center gap-1.5 rounded px-3 py-1.5 text-sm font-medium transition ${
                mode === 'remove'
                  ? 'bg-white text-amber-700 shadow-sm dark:bg-slate-900 dark:text-amber-300'
                  : 'text-slate-600 hover:bg-white/60 dark:text-slate-300'
              }`}
            >
              <Minus size={14} />
              {t('inventory.adjust.modeRemove')}
            </button>
          </div>

          <SelectField
            label={t('inventory.adjust.warehouse')}
            value={warehouseId}
            onChange={setWarehouseId}
            options={warehouses.map((w) => ({ value: w.id, label: `${w.name} (${w.code})` }))}
            placeholder={t('inventory.adjust.warehousePlaceholder')}
            disabled={
              presetWarehouseId !== undefined &&
              presetWarehouseId !== null &&
              presetWarehouseId !== ''
            }
            required
          />

          {lots.length > 0 && (
            <SelectField
              label={t('inventory.adjust.lot')}
              hint={t('inventory.adjust.lotHint')}
              value={lotId}
              onChange={setLotId}
              options={[
                { value: '', label: t('inventory.adjust.noLot') },
                ...lots.map((l) => ({
                  value: l.id,
                  label: `${l.lotNumber}${l.expiryDate ? ` · ${new Date(l.expiryDate).toLocaleDateString(locale)}` : ''}${l.isBlocked ? ' · 🔒' : ''}`,
                })),
              ]}
            />
          )}

          <div className="grid grid-cols-2 gap-3">
            <Input
              label={t('inventory.adjust.quantity')}
              type="number"
              step="0.0001"
              min="0"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
            />
            {mode === 'add' && (
              <Input
                label={t('inventory.adjust.unitCost')}
                type="number"
                step="0.0001"
                min="0"
                value={unitCost}
                onChange={(e) => setUnitCost(e.target.value)}
                placeholder={
                  currentAvgCost !== null && currentAvgCost !== undefined
                    ? String(currentAvgCost)
                    : ''
                }
              />
            )}
          </div>

          <SelectField
            label={t('inventory.adjust.reasonCode')}
            hint={t('inventory.adjust.reasonHint')}
            value={reasonCodeId}
            onChange={setReasonCodeId}
            options={[
              { value: '', label: t('inventory.adjust.noReason') },
              ...reasons.map((r) => ({ value: r.id, label: `${r.name} (${r.code})` })),
            ]}
          />

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('inventory.adjust.notes')}
            </label>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('inventory.adjust.notesPlaceholder')}
              className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder-slate-500"
            />
          </div>

          {currentOnHand !== null && currentOnHand !== undefined && (
            <div
              className={`rounded-lg border p-3 ${
                wouldGoNegative
                  ? 'border-red-200 bg-red-50/60 dark:border-red-500/30 dark:bg-red-500/10'
                  : 'border-slate-200 bg-slate-50/60 dark:border-slate-700 dark:bg-slate-800/30'
              }`}
            >
              <div className="flex items-start gap-2">
                {wouldGoNegative ? (
                  <AlertTriangle
                    size={14}
                    className="mt-0.5 shrink-0 text-red-600 dark:text-red-400"
                  />
                ) : (
                  <Info size={14} className="mt-0.5 shrink-0 text-slate-600 dark:text-slate-400" />
                )}
                <div className="flex-1 space-y-1 text-xs">
                  <div className="flex justify-between text-slate-700 dark:text-slate-300">
                    <span>{t('inventory.adjust.currentOnHand')}</span>
                    <span className="font-mono">{currentOnHand}</span>
                  </div>
                  <div className="flex justify-between text-slate-700 dark:text-slate-300">
                    <span>{t('inventory.adjust.delta')}</span>
                    <span
                      className={`font-mono font-semibold ${delta > 0 ? 'text-emerald-700 dark:text-emerald-300' : 'text-amber-700 dark:text-amber-300'}`}
                    >
                      {delta > 0 ? '+' : ''}
                      {delta}
                    </span>
                  </div>
                  <div
                    className={`flex justify-between font-medium ${wouldGoNegative ? 'text-red-700 dark:text-red-300' : 'text-slate-900 dark:text-slate-100'}`}
                  >
                    <span>{t('inventory.adjust.projectedOnHand')}</span>
                    <span className="font-mono">{projectedOnHand}</span>
                  </div>
                  {wouldGoNegative && (
                    <div className="mt-1 text-[11px] text-red-700 dark:text-red-300">
                      {t('inventory.adjust.negativeWarning')}
                    </div>
                  )}
                  {currentAvgCost !== null && currentAvgCost !== undefined && mode === 'add' && (
                    <div className="mt-2 flex items-center gap-1 text-[11px] text-slate-500 dark:text-slate-400">
                      <Boxes size={11} />
                      {t('inventory.adjust.avgCostHint', {
                        current: currentAvgCost.toFixed(4),
                        currency,
                      })}
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-3 dark:border-slate-800">
          <button
            type="button"
            onClick={requestClose}
            className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel')}
          </button>
          <Button onClick={handleSubmit} isLoading={adjustMutation.isPending} disabled={!canSubmit}>
            {mode === 'add'
              ? t('inventory.adjust.confirmAdd')
              : t('inventory.adjust.confirmRemove')}
          </Button>
        </div>
      </div>
    </div>
  );
};

interface SelectFieldProps {
  label: string;
  hint?: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
}

const SelectField = ({
  label,
  hint,
  value,
  onChange,
  options,
  placeholder,
  disabled,
  required,
}: SelectFieldProps) => (
  <div>
    <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
      {label}
      {required && <span className="text-red-500"> *</span>}
    </label>
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:disabled:bg-slate-800"
    >
      {placeholder && (
        <option value="" disabled>
          {placeholder}
        </option>
      )}
      {options.map((opt) => (
        <option key={opt.value || '__none'} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
    {hint && <p className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">{hint}</p>}
  </div>
);
