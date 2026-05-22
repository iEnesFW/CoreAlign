import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, Trash2, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/features/orders/ui/ProductPicker';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import {
  useAdjustStock,
  useIssueStock,
  useReceiveStock,
  useStockItemsQuery,
} from '../hooks/useInventoryQueries';

export type StockVoucherType = 'receive' | 'issue' | 'count';

interface Props {
  type: StockVoucherType;
  onClose: () => void;
}

interface VoucherLine {
  key: string;
  productId: string;
  quantity: string;
  unitCost: string;
}

const TITLES: Record<StockVoucherType, string> = {
  receive: 'Stok Giriş Fişi',
  issue: 'Stok Çıkış Fişi',
  count: 'Stok Sayım Fişi',
};

const newLine = (): VoucherLine => ({
  key: crypto.randomUUID(),
  productId: '',
  quantity: '',
  unitCost: '',
});

export const StockVoucherModal = ({ type, onClose }: Props) => {
  const { t } = useTranslation();
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const warehousesQuery = useWarehousesQuery(true);
  const receiveMutation = useReceiveStock();
  const issueMutation = useIssueStock();
  const adjustMutation = useAdjustStock();

  const [warehouseId, setWarehouseId] = useState('');
  const [reference, setReference] = useState('');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<VoucherLine[]>([newLine()]);
  const [submitting, setSubmitting] = useState(false);

  const products = productsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];

  // For count vouchers, current on-hand per product in the chosen warehouse is
  // needed to derive the adjustment delta (counted − on-hand).
  const onHandQuery = useStockItemsQuery(
    { warehouseId: warehouseId || undefined, page: 1, pageSize: 500 },
    type === 'count' && Boolean(warehouseId),
  );
  const onHandByProduct = useMemo(() => {
    const map = new Map<string, number>();
    if (type !== 'count') return map;
    for (const it of onHandQuery.data?.data?.items ?? []) {
      map.set(it.productId, (map.get(it.productId) ?? 0) + it.onHand);
    }
    return map;
  }, [type, onHandQuery.data]);

  const updateLine = (key: string, patch: Partial<VoucherLine>) =>
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)));

  const addLine = () => setLines((prev) => [...prev, newLine()]);
  const removeLine = (key: string) =>
    setLines((prev) => (prev.length === 1 ? prev : prev.filter((l) => l.key !== key)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!warehouseId) {
      toast.error(t('inventory.voucher.warehouseRequired', { defaultValue: 'Depo seçiniz.' }));
      return;
    }
    const validLines = lines.filter((l) => l.productId && Number(l.quantity) > 0);
    if (validLines.length === 0) {
      toast.error(
        t('inventory.voucher.linesRequired', { defaultValue: 'En az bir geçerli satır ekleyin.' }),
      );
      return;
    }

    setSubmitting(true);
    const noteText = [reference.trim(), notes.trim()].filter(Boolean).join(' — ') || null;
    const results = await Promise.allSettled(
      validLines.map((l) => {
        const qty = Number(l.quantity);
        if (type === 'receive') {
          return receiveMutation.mutateAsync({
            productId: l.productId,
            warehouseId,
            quantity: qty,
            unitCost: Number(l.unitCost) || 0,
            reference: reference.trim() || null,
            notes: notes.trim() || null,
          });
        }
        if (type === 'issue') {
          return issueMutation.mutateAsync({
            productId: l.productId,
            warehouseId,
            quantity: qty,
            reference: reference.trim() || null,
            notes: notes.trim() || null,
          });
        }
        const delta = qty - (onHandByProduct.get(l.productId) ?? 0);
        if (delta === 0) return Promise.resolve(null);
        return adjustMutation.mutateAsync({
          productId: l.productId,
          warehouseId,
          delta,
          notes: noteText ? `Sayım: ${noteText}` : 'Sayım düzeltmesi',
        });
      }),
    );
    setSubmitting(false);

    const failed = results.filter((r) => r.status === 'rejected').length;
    const ok = results.length - failed;
    if (failed === 0) {
      toast.success(
        t('inventory.voucher.posted', { defaultValue: '{{count}} satır işlendi.', count: ok }),
      );
      onClose();
    } else {
      const firstError = results.find((r) => r.status === 'rejected') as
        | PromiseRejectedResult
        | undefined;
      if (firstError) toastApiError(firstError.reason);
      toast.warning(
        t('inventory.voucher.partial', {
          defaultValue: '{{ok}} satır işlendi, {{failed}} satır başarısız.',
          ok,
          failed,
        }),
      );
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="flex max-h-[92vh] w-full max-w-2xl flex-col rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t(`inventory.voucher.title.${type}`, { defaultValue: TITLES[type] })}
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
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('inventory.voucher.warehouse', { defaultValue: 'Depo' })} *
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
                  {t('inventory.voucher.reference', { defaultValue: 'Belge No / Referans' })}
                </label>
                <input
                  type="text"
                  value={reference}
                  onChange={(e) => setReference(e.target.value)}
                  maxLength={64}
                  className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('inventory.voucher.notes', { defaultValue: 'Açıklama' })}
                </label>
                <input
                  type="text"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  maxLength={200}
                  className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                />
              </div>
            </div>

            <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                  <tr>
                    <th className="px-2 py-1.5 text-left">
                      {t('inventory.voucher.product', { defaultValue: 'Ürün' })}
                    </th>
                    <th className="w-28 px-2 py-1.5 text-right">
                      {type === 'count'
                        ? t('inventory.voucher.counted', { defaultValue: 'Sayılan' })
                        : t('inventory.voucher.quantity', { defaultValue: 'Miktar' })}
                    </th>
                    {type === 'receive' && (
                      <th className="w-28 px-2 py-1.5 text-right">
                        {t('inventory.voucher.unitCost', { defaultValue: 'Birim Maliyet' })}
                      </th>
                    )}
                    {type === 'count' && (
                      <th className="w-24 px-2 py-1.5 text-right">
                        {t('inventory.voucher.onHand', { defaultValue: 'Mevcut' })}
                      </th>
                    )}
                    <th className="w-8 px-2 py-1.5"></th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((l) => {
                    const onHand = onHandByProduct.get(l.productId) ?? 0;
                    return (
                      <tr key={l.key} className="border-t border-slate-100 dark:border-slate-800">
                        <td className="px-2 py-1.5">
                          <ProductPicker
                            products={products}
                            value={l.productId}
                            onSelect={(productId) => updateLine(l.key, { productId })}
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <input
                            type="number"
                            min={0}
                            step="any"
                            value={l.quantity}
                            onChange={(e) => updateLine(l.key, { quantity: e.target.value })}
                            className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                          />
                        </td>
                        {type === 'receive' && (
                          <td className="px-2 py-1.5">
                            <input
                              type="number"
                              min={0}
                              step="any"
                              value={l.unitCost}
                              onChange={(e) => updateLine(l.key, { unitCost: e.target.value })}
                              className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                            />
                          </td>
                        )}
                        {type === 'count' && (
                          <td className="px-2 py-1.5 text-right font-mono text-xs text-slate-500 dark:text-slate-400">
                            {l.productId ? onHand : '—'}
                          </td>
                        )}
                        <td className="px-2 py-1.5 text-center">
                          <button
                            type="button"
                            onClick={() => removeLine(l.key)}
                            disabled={lines.length === 1}
                            className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 disabled:opacity-30 dark:hover:bg-rose-500/10"
                            aria-label={t('common.delete', { defaultValue: 'Sil' })}
                          >
                            <Trash2 size={13} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <button
              type="button"
              onClick={addLine}
              className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Plus size={12} />
              {t('inventory.voucher.addLine', { defaultValue: 'Satır ekle' })}
            </button>
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
              disabled={submitting}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {submitting
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('inventory.voucher.post', { defaultValue: 'Fişi İşle' })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
