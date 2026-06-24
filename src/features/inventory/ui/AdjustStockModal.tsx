import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Boxes, Info, Minus, Plus } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
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
  const requestClose = useModalClose(dirty, onClose, false);

  return (
    <Modal
      open={open}
      title={t('inventory.adjust.title')}
      subtitle={`${productSku} · ${productName}`}
      icon={<Boxes size={18} />}
      onClose={requestClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={requestClose}>
            {t('common.cancel')}
          </Button>
          <Button onClick={handleSubmit} isLoading={adjustMutation.isPending} disabled={!canSubmit}>
            {mode === 'add'
              ? t('inventory.adjust.confirmAdd')
              : t('inventory.adjust.confirmRemove')}
          </Button>
        </>
      }
    >
      <div className="space-y-4" onChange={() => setDirty(true)}>
        <div className="flex gap-1 rounded-lg border border-slate-200 bg-slate-50 p-1 dark:border-slate-700 dark:bg-slate-800/40">
          <button
            type="button"
            onClick={() => setMode('add')}
            className={`flex-1 inline-flex items-center justify-center gap-1.5 rounded px-3 py-1.5 text-sm font-medium transition ${
              mode === 'add'
                ? 'bg-white text-success-700 shadow-sm dark:bg-slate-900 dark:text-success-300'
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
                ? 'bg-white text-warning-700 shadow-sm dark:bg-slate-900 dark:text-warning-300'
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
            className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder-slate-500"
          />
        </div>

        {currentOnHand !== null && currentOnHand !== undefined && (
          <div
            className={`rounded-lg border p-3 ${
              wouldGoNegative
                ? 'border-danger-200 bg-danger-50/60 dark:border-danger-500/30 dark:bg-danger-500/10'
                : 'border-slate-200 bg-slate-50/60 dark:border-slate-700 dark:bg-slate-800/30'
            }`}
          >
            <div className="flex items-start gap-2">
              {wouldGoNegative ? (
                <AlertTriangle
                  size={14}
                  className="mt-0.5 shrink-0 text-danger-600 dark:text-danger-400"
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
                    className={`font-mono font-semibold ${delta > 0 ? 'text-success-700 dark:text-success-300' : 'text-warning-700 dark:text-warning-300'}`}
                  >
                    {delta > 0 ? '+' : ''}
                    {delta}
                  </span>
                </div>
                <div
                  className={`flex justify-between font-medium ${wouldGoNegative ? 'text-danger-700 dark:text-danger-300' : 'text-slate-900 dark:text-slate-100'}`}
                >
                  <span>{t('inventory.adjust.projectedOnHand')}</span>
                  <span className="font-mono">{projectedOnHand}</span>
                </div>
                {wouldGoNegative && (
                  <div className="mt-1 text-[11px] text-danger-700 dark:text-danger-300">
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
    </Modal>
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
      {required && <span className="text-danger-500"> *</span>}
    </label>
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className={fieldBaseClasses(false)}
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
