import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Flame, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import { useRecordOrderScrap } from '@/features/orders/hooks/useOrderQueries';
import type { Order } from '@/features/orders/model/order.types';

interface Props {
  order: Order;
  onClose: () => void;
}

export const OrderScrapModal = ({ order, onClose }: Props) => {
  const { t } = useTranslation();
  const warehousesQuery = useWarehousesQuery(true);
  const scrapMutation = useRecordOrderScrap();

  const scrappableLines = useMemo(
    () => order.lines.filter((l) => l.quantityRemainingToShip > 0),
    [order.lines],
  );

  const [orderLineId, setOrderLineId] = useState(scrappableLines[0]?.id ?? '');
  const [quantity, setQuantity] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [notes, setNotes] = useState('');

  const warehouses = warehousesQuery.data?.data ?? [];
  const selectedLine = order.lines.find((l) => l.id === orderLineId);
  const maxQty = selectedLine?.quantityRemainingToShip ?? 0;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const qty = Number(quantity) || 0;
    if (!orderLineId || qty <= 0 || !warehouseId) {
      toast.error(
        t('orders.scrap.invalid', { defaultValue: 'Kalem, depo ve geçerli miktar giriniz.' }),
      );
      return;
    }
    if (qty > maxQty) {
      toast.error(
        t('orders.scrap.exceeds', {
          defaultValue: 'Fire miktarı kalan miktarı ({{max}}) aşamaz.',
          max: maxQty,
        }),
      );
      return;
    }
    try {
      await scrapMutation.mutateAsync({
        id: order.id,
        orderLineId,
        quantity: qty,
        warehouseId,
        notes: notes.trim() || null,
      });
      toast.success(t('orders.scrap.done', { defaultValue: 'Fire kaydedildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const inputClass =
    'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-md rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="inline-flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
            <Flame size={15} className="text-orange-500" />
            {t('orders.scrap.title', { defaultValue: 'Fire Gir' })}
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
          {scrappableLines.length === 0 ? (
            <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
              {t('orders.scrap.noLines', { defaultValue: 'Fire girilebilecek kalem yok.' })}
            </p>
          ) : (
            <>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('orders.scrap.line', { defaultValue: 'Kalem' })} *
                </label>
                <select
                  value={orderLineId}
                  onChange={(e) => setOrderLineId(e.target.value)}
                  className={inputClass}
                >
                  {scrappableLines.map((l) => (
                    <option key={l.id} value={l.id}>
                      {l.productName} — {t('orders.scrap.remaining', { defaultValue: 'kalan' })}:{' '}
                      {l.quantityRemainingToShip}
                    </option>
                  ))}
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                    {t('orders.scrap.quantity', { defaultValue: 'Fire Miktarı' })} *
                  </label>
                  <input
                    type="number"
                    min={0}
                    max={maxQty}
                    step="any"
                    value={quantity}
                    onChange={(e) => setQuantity(e.target.value)}
                    className={`${inputClass} text-right`}
                  />
                  <p className="mt-0.5 text-[10px] text-slate-400">
                    {t('orders.scrap.max', { defaultValue: 'En fazla' })}: {maxQty}
                  </p>
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                    {t('orders.scrap.warehouse', { defaultValue: 'Depo' })} *
                  </label>
                  <select
                    value={warehouseId}
                    onChange={(e) => setWarehouseId(e.target.value)}
                    className={inputClass}
                  >
                    <option value="">
                      {t('orders.scrap.selectWarehouse', { defaultValue: 'Seçiniz…' })}
                    </option>
                    {warehouses.map((w) => (
                      <option key={w.id} value={w.id}>
                        {w.name} ({w.code})
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('orders.scrap.notes', { defaultValue: 'Fire Nedeni / Açıklama' })}
                </label>
                <input
                  type="text"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  maxLength={200}
                  className={inputClass}
                  placeholder={t('orders.scrap.notesPlaceholder', {
                    defaultValue: 'Örn. üretim hatası, hasar…',
                  })}
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
              disabled={scrapMutation.isPending || scrappableLines.length === 0}
              className="rounded bg-orange-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-orange-700 disabled:opacity-50"
            >
              {scrapMutation.isPending
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('orders.scrap.submit', { defaultValue: 'Fire Kaydet' })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
