import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { AlertTriangle, Factory, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import {
  useProductComponentsQuery,
  useProductsQuery,
} from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/features/orders/ui/ProductPicker';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="flex max-h-[92vh] w-full max-w-xl flex-col rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="inline-flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
            <Factory size={15} />
            {t('inventory.production.title', { defaultValue: 'Üretim Fişi' })}
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

        <form onSubmit={submit} className="flex min-h-0 flex-1 flex-col">
          <div className="space-y-3 overflow-y-auto p-4">
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('inventory.production.product', { defaultValue: 'Üretilecek Ürün' })} *
              </label>
              <div className="mt-1">
                <ProductPicker
                  products={products}
                  value={productId}
                  onSelect={(id) => setProductId(id)}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('inventory.production.warehouse', { defaultValue: 'Depo' })} *
                </label>
                <select
                  value={warehouseId}
                  onChange={(e) => setWarehouseId(e.target.value)}
                  className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                >
                  <option value="">
                    {t('inventory.voucher.selectWarehouse', { defaultValue: 'Seçiniz…' })}
                  </option>
                  {warehouses.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name} ({w.code})
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('inventory.production.quantity', { defaultValue: 'Üretim Miktarı' })} *
                </label>
                <input
                  type="number"
                  min={0}
                  step="any"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('inventory.production.notes', { defaultValue: 'Açıklama' })}
              </label>
              <input
                type="text"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                maxLength={200}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              />
            </div>

            <div>
              <div className="mb-1 text-xs font-semibold text-slate-700 dark:text-slate-300">
                {t('inventory.production.components', {
                  defaultValue: 'Tüketilecek Bileşenler (Reçete)',
                })}
              </div>
              {productId && components.length === 0 ? (
                <div className="inline-flex items-center gap-1.5 rounded bg-amber-50 px-2 py-1.5 text-xs text-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
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
          </div>

          <div className="flex justify-end gap-2 border-t border-slate-200 px-4 py-3 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('common.cancel', { defaultValue: 'İptal' })}
            </button>
            <button
              type="submit"
              disabled={produceMutation.isPending}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {produceMutation.isPending
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('inventory.production.produce', { defaultValue: 'Üret' })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
