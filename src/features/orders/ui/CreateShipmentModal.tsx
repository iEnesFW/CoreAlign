import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Package, Truck } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
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
  const requestClose = useModalClose(dirty, onClose, false);

  return (
    <Modal
      open
      title={t('orders.shipments.createTitle')}
      subtitle={`${order.orderNumber} · ${order.customerName}`}
      icon={<Truck size={18} />}
      onClose={requestClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={requestClose}>
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            form="create-shipment-form"
            isLoading={createMutation.isPending}
            disabled={createMutation.isPending || !warehouseId || totalToShip <= 0}
          >
            <Truck size={14} />
            {t('orders.actions.createShipment')}
          </Button>
        </>
      }
    >
      <form
        id="create-shipment-form"
        onSubmit={handleSubmit}
        onChange={() => setDirty(true)}
        className="space-y-4"
      >
        <Select
          label={`${t('inventory.adjust.warehouse')} *`}
          value={warehouseId}
          onChange={(e) => setWarehouseId(e.target.value)}
        >
          <option value="" disabled>
            {t('inventory.adjust.warehousePlaceholder')}
          </option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name} ({w.code})
            </option>
          ))}
        </Select>

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
                        className="mt-1 h-4 w-4 rounded border-slate-300 text-primary-600"
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
                        onChange={(e) => updateSelection(idx, { quantity: Number(e.target.value) })}
                        className={`${fieldBaseClasses(false)} w-24 px-2 text-right`}
                      />
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        <Textarea
          label={t('inventory.adjust.notes')}
          rows={2}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />

        <div className="rounded border border-primary-200 bg-primary-50/60 px-3 py-2 text-xs text-primary-800 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-200">
          <Truck size={11} className="mr-1 inline" />
          {t('orders.shipments.title')}: {fmtNumber(totalToShip, locale)} kalem
        </div>
      </form>
    </Modal>
  );
};
