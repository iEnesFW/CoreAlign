import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { PackageCheck } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useReceivePurchaseOrder } from '../hooks/usePurchaseOrders';
import type { PurchaseOrder } from '../model/purchaseOrder.types';

interface Props {
  order: PurchaseOrder;
  onClose: () => void;
}

export const ReceivePurchaseOrderModal = ({ order, onClose }: Props) => {
  const { t } = useTranslation();
  const warehousesQuery = useWarehousesQuery(true);
  const receiveMutation = useReceivePurchaseOrder();

  const idempotencyKey = useMemo(() => crypto.randomUUID(), []);

  const receivableLines = order.lines.filter((l) => l.quantityRemainingToReceive > 0);
  const warehouses = warehousesQuery.data?.data ?? [];

  const [warehouseId, setWarehouseId] = useState(order.warehouseId ?? '');
  const [notes, setNotes] = useState('');
  const [qtys, setQtys] = useState<Record<string, string>>(() =>
    Object.fromEntries(receivableLines.map((l) => [l.id, String(l.quantityRemainingToReceive)])),
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const lines = receivableLines
      .map((l) => ({ orderLineId: l.id, quantity: Number(qtys[l.id]) || 0 }))
      .filter((l) => l.quantity > 0);
    if (lines.length === 0) {
      toast.error(t('po.receive.nothing', { defaultValue: 'Teslim alınacak miktar giriniz.' }));
      return;
    }
    const overReceipt = receivableLines.some(
      (l) => (Number(qtys[l.id]) || 0) > l.quantityRemainingToReceive,
    );
    if (overReceipt) {
      toast.error(t('po.receive.over', { defaultValue: 'Kalan miktardan fazla teslim alınamaz.' }));
      return;
    }
    if (!warehouseId) {
      toast.error(t('po.receive.warehouseRequired', { defaultValue: 'Depo seçiniz.' }));
      return;
    }
    try {
      await receiveMutation.mutateAsync({
        id: order.id,
        lines,
        warehouseId,
        notes: notes.trim() || null,
        idempotencyKey,
      });
      toast.success(t('po.receive.done', { defaultValue: 'Mal kabul yapıldı, stok güncellendi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={t('po.receive.title', { defaultValue: 'Mal Kabul' })}
      subtitle={order.poNumber}
      icon={<PackageCheck size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button
            type="submit"
            form="receive-purchase-order-form"
            isLoading={receiveMutation.isPending}
            disabled={receivableLines.length === 0}
            className="bg-success-600 shadow-success-600/30 hover:bg-success-500 hover:shadow-success-600/40"
          >
            {receiveMutation.isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('po.receive.submit', { defaultValue: 'Mal Kabul Et' })}
          </Button>
        </>
      }
    >
      <form id="receive-purchase-order-form" onSubmit={submit} className="space-y-3">
        {receivableLines.length === 0 ? (
          <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('po.receive.allReceived', { defaultValue: 'Tüm kalemler teslim alınmış.' })}
          </p>
        ) : (
          <>
            <Select
              label={`${t('po.receive.warehouse', { defaultValue: 'Teslim Deposu' })} *`}
              value={warehouseId}
              onChange={(e) => setWarehouseId(e.target.value)}
            >
              <option value="">
                {t('po.receive.selectWarehouse', { defaultValue: 'Seçiniz…' })}
              </option>
              {warehouses.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))}
            </Select>

            <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                  <tr>
                    <th className="px-2 py-1.5 text-left">
                      {t('po.receive.product', { defaultValue: 'Ürün' })}
                    </th>
                    <th className="w-24 px-2 py-1.5 text-right">
                      {t('po.receive.remaining', { defaultValue: 'Kalan' })}
                    </th>
                    <th className="w-28 px-2 py-1.5 text-right">
                      {t('po.receive.qty', { defaultValue: 'Teslim Alınan' })}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {receivableLines.map((l) => (
                    <tr key={l.id} className="border-t border-slate-100 dark:border-slate-800">
                      <td className="px-2 py-1.5">
                        <div className="font-medium text-slate-800 dark:text-slate-100">
                          {l.productName}
                        </div>
                        <div className="font-mono text-[10px] text-slate-400 dark:text-slate-500">
                          {l.productSku}
                        </div>
                      </td>
                      <td className="px-2 py-1.5 text-right font-mono text-slate-600 dark:text-slate-300">
                        {l.quantityRemainingToReceive}
                      </td>
                      <td className="px-2 py-1.5">
                        <input
                          type="number"
                          min={0}
                          max={l.quantityRemainingToReceive}
                          step="any"
                          value={qtys[l.id] ?? ''}
                          onChange={(e) => setQtys((p) => ({ ...p, [l.id]: e.target.value }))}
                          aria-label={t('po.receive.qty', { defaultValue: 'Teslim Alınan' })}
                          className={`${fieldBaseClasses(false)} text-right`}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <Input
              label={t('po.receive.notes', { defaultValue: 'Açıklama / İrsaliye No' })}
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              maxLength={200}
            />
          </>
        )}
      </form>
    </Modal>
  );
};
