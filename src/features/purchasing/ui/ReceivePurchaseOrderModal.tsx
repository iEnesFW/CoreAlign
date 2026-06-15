import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { PackageCheck, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
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
      });
      toast.success(t('po.receive.done', { defaultValue: 'Mal kabul yapıldı, stok güncellendi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const inputClass =
    'w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="flex max-h-[92vh] w-full max-w-xl flex-col rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="inline-flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
            <PackageCheck size={15} className="text-emerald-500" />
            {t('po.receive.title', { defaultValue: 'Mal Kabul' })} — {order.poNumber}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={t('common.close', { defaultValue: 'Kapat' })}
          >
            <X size={16} />
          </button>
        </div>

        <form onSubmit={submit} className="space-y-3 p-4">
          {receivableLines.length === 0 ? (
            <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
              {t('po.receive.allReceived', { defaultValue: 'Tüm kalemler teslim alınmış.' })}
            </p>
          ) : (
            <>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('po.receive.warehouse', { defaultValue: 'Teslim Deposu' })} *
                </label>
                <select
                  value={warehouseId}
                  onChange={(e) => setWarehouseId(e.target.value)}
                  className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                >
                  <option value="">
                    {t('po.receive.selectWarehouse', { defaultValue: 'Seçiniz…' })}
                  </option>
                  {warehouses.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name} ({w.code})
                    </option>
                  ))}
                </select>
              </div>

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
                            className={inputClass}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('po.receive.notes', { defaultValue: 'Açıklama / İrsaliye No' })}
                </label>
                <input
                  type="text"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  maxLength={200}
                  className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                />
              </div>
            </>
          )}

          <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('common.cancel', { defaultValue: 'İptal' })}
            </button>
            <button
              type="submit"
              disabled={receiveMutation.isPending || receivableLines.length === 0}
              className="rounded bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              {receiveMutation.isPending
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('po.receive.submit', { defaultValue: 'Mal Kabul Et' })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
