import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Package, Truck, X } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import { useCreateShipment } from '../hooks/useOrderQueries';
import type { Order } from '../model/order.types';

interface Props {
  order: Order;
  onClose: () => void;
}

interface LineSelection {
  orderLineId: string;
  selected: boolean;
  quantity: number;
  notes: string;
}

const fmtNumber = (n: number, locale: string) =>
  new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(n);

export const CreateShipmentModal = ({ order, onClose }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const warehousesQuery = useWarehousesQuery(true);
  const createMutation = useCreateShipment();
  const warehouses = warehousesQuery.data?.data ?? [];
  const defaultWarehouse = warehouses.find((w) => w.isDefault);

  const [warehouseId, setWarehouseId] = useState(defaultWarehouse?.id ?? warehouses[0]?.id ?? '');
  const [notes, setNotes] = useState('');
  const [lineSelections, setLineSelections] = useState<LineSelection[]>(
    order.lines.map((l) => ({
      orderLineId: l.id,
      selected: l.quantityRemainingToShip > 0,
      quantity: l.quantityRemainingToShip,
      notes: '',
    })),
  );

  const shippableLines = useMemo(
    () => order.lines.filter((l) => l.quantityRemainingToShip > 0),
    [order.lines],
  );

  const updateSelection = (idx: number, patch: Partial<LineSelection>) =>
    setLineSelections((prev) => prev.map((s, i) => (i === idx ? { ...s, ...patch } : s)));

  const handleSubmit = async () => {
    const selectedLines = lineSelections
      .filter((s) => s.selected && s.quantity > 0)
      .map((s) => ({
        orderLineId: s.orderLineId,
        quantity: s.quantity,
        notes: s.notes || null,
      }));
    if (selectedLines.length === 0) {
      toast.error(t('orders.shipments.selectLines'));
      return;
    }
    if (!warehouseId) return;

    try {
      await createMutation.mutateAsync({
        orderId: order.id,
        warehouseId,
        lines: selectedLines,
        notes: notes || null,
      });
      toast.success(t('orders.actions.createShipment'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const totalToShip = lineSelections
    .filter((s) => s.selected)
    .reduce((sum, s) => sum + (Number.isFinite(s.quantity) ? s.quantity : 0), 0);

  const [dirty, setDirty] = useState(false);
  const requestClose = useModalClose(dirty, onClose);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={requestClose}
      role="presentation"
    >
      <div
        className="w-full max-w-2xl rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        onChange={() => setDirty(true)}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-3 dark:border-slate-800">
          <div>
            <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
              {t('orders.shipments.createTitle')}
            </h2>
            <p className="text-[11px] text-slate-500 dark:text-slate-400">
              {order.orderNumber} · {order.customerName}
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
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('inventory.adjust.warehouse')} *
            </label>
            <select
              value={warehouseId}
              onChange={(e) => setWarehouseId(e.target.value)}
              className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              <option value="" disabled>
                {t('inventory.adjust.warehousePlaceholder')}
              </option>
              {warehouses.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))}
            </select>
          </div>

          <div className="rounded-lg border border-slate-200 dark:border-slate-800">
            <div className="border-b border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-700 dark:border-slate-800 dark:bg-slate-900/40 dark:text-slate-200">
              <Package size={12} className="mr-1 inline" />
              {t('orders.shipments.selectLines')}
            </div>
            {shippableLines.length === 0 ? (
              <div className="px-3 py-4 text-center text-xs text-slate-500">
                {t('orders.shipments.empty')}
              </div>
            ) : (
              <ul className="divide-y divide-slate-200 dark:divide-slate-800">
                {order.lines.map((line, idx) => {
                  const sel = lineSelections[idx];
                  const disabled = line.quantityRemainingToShip <= 0;
                  return (
                    <li key={line.id} className={`px-3 py-2 ${disabled ? 'opacity-50' : ''}`}>
                      <div className="flex items-start gap-3">
                        <input
                          type="checkbox"
                          disabled={disabled}
                          checked={sel.selected}
                          onChange={(e) => updateSelection(idx, { selected: e.target.checked })}
                          className="mt-1 h-4 w-4 rounded border-slate-300 text-indigo-600"
                        />
                        <div className="flex-1">
                          <div className="text-sm font-medium text-slate-900 dark:text-slate-100">
                            {line.productSku} · {line.productName}
                          </div>
                          <div className="text-[10px] text-slate-500">
                            {t('orders.shipments.remaining', {
                              value: fmtNumber(line.quantityRemainingToShip, locale),
                            })}
                            {' · '}
                            {t('orders.fields.lines').toLowerCase()}:{' '}
                            {fmtNumber(line.quantity, locale)}
                          </div>
                        </div>
                        <input
                          type="number"
                          min="0"
                          max={line.quantityRemainingToShip}
                          step="0.0001"
                          disabled={disabled || !sel.selected}
                          value={sel.quantity}
                          onChange={(e) =>
                            updateSelection(idx, { quantity: Number(e.target.value) })
                          }
                          className="w-24 rounded border border-slate-200 px-2 py-1 text-right text-sm dark:border-slate-700 dark:bg-slate-900"
                        />
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('inventory.adjust.notes')}
            </label>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
            />
          </div>

          <div className="rounded border border-indigo-200 bg-indigo-50/60 px-3 py-2 text-xs text-indigo-800 dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-200">
            <Truck size={11} className="mr-1 inline" />
            {t('orders.shipments.title')}: {fmtNumber(totalToShip, locale)} kalem
          </div>
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-3 dark:border-slate-800">
          <button
            type="button"
            onClick={requestClose}
            className="rounded px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel')}
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={createMutation.isPending || !warehouseId || totalToShip <= 0}
            className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            <Truck size={14} />
            {t('orders.actions.createShipment')}
          </button>
        </div>
      </div>
    </div>
  );
};
