import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { AlertTriangle, Factory } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import {
  useProductComponentsQuery,
  useProductsQuery,
} from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Label } from '@/shared/ui/Label/Label';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useProduce } from '../hooks/useInventoryQueries';

interface Props {
  onClose: () => void;
}

const fmt = (n: number, locale: string) =>
  new Intl.NumberFormat(locale, { minimumFractionDigits: 0, maximumFractionDigits: 4 }).format(n);

export const ProductionModal = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const warehousesQuery = useWarehousesQuery(true);
  const produceMutation = useProduce();

  const [productId, setProductId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [quantity, setQuantity] = useState('');
  const [notes, setNotes] = useState('');

  const products = productsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];
  const componentsQuery = useProductComponentsQuery(productId || null);
  const components = componentsQuery.data?.data ?? [];

  const qtyNum = Number(quantity) || 0;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!productId || !warehouseId || qtyNum <= 0) {
      toast.error(
        t('inventory.production.invalid', {
          defaultValue: 'Ürün, depo ve geçerli miktar giriniz.',
        }),
      );
      return;
    }
    if (components.length === 0) {
      toast.error(
        t('inventory.production.noBom', { defaultValue: 'Bu ürünün reçetesi (BOM) yok.' }),
      );
      return;
    }
    try {
      await produceMutation.mutateAsync({
        productId,
        warehouseId,
        quantity: qtyNum,
        notes: notes.trim() || null,
      });
      toast.success(t('inventory.production.done', { defaultValue: 'Üretim işlendi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      title={t('inventory.production.title', { defaultValue: 'Üretim Fişi' })}
      icon={<Factory size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="production-modal-form" isLoading={produceMutation.isPending}>
            {produceMutation.isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('inventory.production.produce', { defaultValue: 'Üret' })}
          </Button>
        </>
      }
    >
      <form id="production-modal-form" onSubmit={submit} className="space-y-3">
        <div className="flex flex-col gap-1.5">
          <Label required>
            {t('inventory.production.product', { defaultValue: 'Üretilecek Ürün' })}
          </Label>
          <ProductPicker
            products={products}
            value={productId}
            onSelect={(id) => setProductId(id)}
          />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Select
            label={t('inventory.production.warehouse', { defaultValue: 'Depo' })}
            required
            value={warehouseId}
            onChange={(e) => setWarehouseId(e.target.value)}
          >
            <option value="">
              {t('inventory.voucher.selectWarehouse', { defaultValue: 'Seçiniz…' })}
            </option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name} ({w.code})
              </option>
            ))}
          </Select>
          <Input
            label={t('inventory.production.quantity', { defaultValue: 'Üretim Miktarı' })}
            required
            type="number"
            min={0}
            step="any"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            className="text-right"
          />
        </div>

        <Input
          label={t('inventory.production.notes', { defaultValue: 'Açıklama' })}
          type="text"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          maxLength={200}
        />

        <div>
          <div className="mb-1 text-xs font-semibold text-slate-700 dark:text-slate-300">
            {t('inventory.production.components', {
              defaultValue: 'Tüketilecek Bileşenler (Reçete)',
            })}
          </div>
          {productId && components.length === 0 ? (
            <div className="inline-flex items-center gap-1.5 rounded bg-warning-50 px-2 py-1.5 text-xs text-warning-700 dark:bg-warning-500/10 dark:text-warning-300">
              <AlertTriangle size={12} />
              {t('inventory.production.noBom', {
                defaultValue: 'Bu ürünün reçetesi (BOM) yok.',
              })}
            </div>
          ) : (
            <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                  <tr>
                    <th className="px-2 py-1.5 text-left">
                      {t('inventory.production.component', { defaultValue: 'Bileşen' })}
                    </th>
                    <th className="px-2 py-1.5 text-right">
                      {t('inventory.production.perUnit', { defaultValue: 'Birim/Adet' })}
                    </th>
                    <th className="px-2 py-1.5 text-right">
                      {t('inventory.production.required', { defaultValue: 'Gerekli' })}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {components.map((c) => (
                    <tr key={c.id} className="border-t border-slate-100 dark:border-slate-800">
                      <td className="px-2 py-1.5">
                        <div className="font-medium text-slate-800 dark:text-slate-100">
                          {c.componentName}
                        </div>
                        <div className="font-mono text-[10px] text-slate-400 dark:text-slate-500">
                          {c.componentSku}
                        </div>
                      </td>
                      <td className="px-2 py-1.5 text-right font-mono text-slate-600 dark:text-slate-400">
                        {fmt(c.quantity, locale)}
                      </td>
                      <td className="px-2 py-1.5 text-right font-mono font-semibold text-slate-800 dark:text-slate-200">
                        {fmt(c.quantity * qtyNum, locale)}
                      </td>
                    </tr>
                  ))}
                  {!productId && (
                    <tr>
                      <td
                        colSpan={3}
                        className="px-2 py-3 text-center text-xs text-slate-500 dark:text-slate-400"
                      >
                        {t('inventory.production.selectProduct', {
                          defaultValue: 'Önce ürün seçiniz.',
                        })}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </form>
    </Modal>
  );
};
