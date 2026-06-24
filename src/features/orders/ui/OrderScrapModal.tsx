import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Flame } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Label } from '@/shared/ui/Label/Label';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
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

  return (
    <Modal
      open
      title={t('orders.scrap.title', { defaultValue: 'Fire Gir' })}
      icon={<Flame size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button
            type="submit"
            form="order-scrap-form"
            isLoading={scrapMutation.isPending}
            disabled={scrappableLines.length === 0}
            className="bg-warning-600 shadow-warning-600/30 hover:bg-warning-500 hover:shadow-warning-600/40"
          >
            {t('orders.scrap.submit', { defaultValue: 'Fire Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="order-scrap-form" onSubmit={submit} className="space-y-3">
        {scrappableLines.length === 0 ? (
          <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('orders.scrap.noLines', { defaultValue: 'Fire girilebilecek kalem yok.' })}
          </p>
        ) : (
          <>
            <Select
              label={`${t('orders.scrap.line', { defaultValue: 'Kalem' })} *`}
              value={orderLineId}
              onChange={(e) => setOrderLineId(e.target.value)}
            >
              {scrappableLines.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.productName} — {t('orders.scrap.remaining', { defaultValue: 'kalan' })}:{' '}
                  {l.quantityRemainingToShip}
                </option>
              ))}
            </Select>

            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <Label>{t('orders.scrap.quantity', { defaultValue: 'Fire Miktarı' })} *</Label>
                <input
                  type="number"
                  min={0}
                  max={maxQty}
                  step="any"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  className={`${fieldBaseClasses(false)} text-right`}
                />
                <p className="mt-0.5 text-[10px] text-slate-400">
                  {t('orders.scrap.max', { defaultValue: 'En fazla' })}: {maxQty}
                </p>
              </div>
              <Select
                label={`${t('orders.scrap.warehouse', { defaultValue: 'Depo' })} *`}
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
              >
                <option value="">
                  {t('orders.scrap.selectWarehouse', { defaultValue: 'Seçiniz…' })}
                </option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code})
                  </option>
                ))}
              </Select>
            </div>

            <Input
              label={t('orders.scrap.notes', { defaultValue: 'Fire Nedeni / Açıklama' })}
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              maxLength={200}
              placeholder={t('orders.scrap.notesPlaceholder', {
                defaultValue: 'Örn. üretim hatası, hasar…',
              })}
            />
          </>
        )}
      </form>
    </Modal>
  );
};
