import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import {
  useAddPriceListItem,
  usePriceListItemsQuery,
  useRemovePriceListItem,
  useUpdatePriceListItem,
} from '@/features/pricing/hooks/usePricingRulesQueries';
import type { PriceListItem } from '@/features/pricing/model/pricingRules.types';
import type { PriceList } from '@/features/master-data/model/masterData.types';

interface Props {
  priceList: PriceList;
}

interface RowDraft {
  id?: string;
  productId: string;
  price: string;
  minQuantity: string;
  maxQuantity: string;
  discountPercent: string;
}

const emptyDraft = (): RowDraft => ({
  productId: '',
  price: '',
  minQuantity: '',
  maxQuantity: '',
  discountPercent: '',
});

const toDraft = (item: PriceListItem): RowDraft => ({
  id: item.id,
  productId: item.productId,
  price: String(item.price),
  minQuantity: item.minQuantity === null ? '' : String(item.minQuantity),
  maxQuantity: item.maxQuantity === null ? '' : String(item.maxQuantity),
  discountPercent: item.discountPercent === null ? '' : String(item.discountPercent),
});

const parseNumber = (raw: string): number | null => {
  const trimmed = raw.trim();
  if (trimmed === '') return null;
  const value = Number(trimmed);
  return Number.isFinite(value) ? value : null;
};

export const PriceListItemsGrid = ({ priceList }: Props) => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const productsQ = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const itemsQ = usePriceListItemsQuery(priceList.id);
  const addMutation = useAddPriceListItem();
  const updateMutation = useUpdatePriceListItem();
  const removeMutation = useRemovePriceListItem(priceList.id);

  const products = useMemo(() => productsQ.data?.data?.items ?? [], [productsQ.data]);
  const items = itemsQ.data?.data ?? [];

  const productNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const p of products) map.set(p.id, `${p.sku} — ${p.name}`);
    return map;
  }, [products]);

  const [draft, setDraft] = useState<RowDraft | null>(null);

  const beginAdd = () => setDraft(emptyDraft());
  const beginEdit = (item: PriceListItem) => setDraft(toDraft(item));
  const cancel = () => setDraft(null);

  const saveDraft = async () => {
    if (!draft) return;
    const price = parseNumber(draft.price);
    if (price === null || price < 0) {
      toast.error(t('Settings.PriceLists.Lines.Errors.PriceRequired'));
      return;
    }
    if (!draft.productId) {
      toast.error(t('Settings.PriceLists.Lines.Errors.ProductRequired'));
      return;
    }
    const min = parseNumber(draft.minQuantity);
    const max = parseNumber(draft.maxQuantity);
    if (min !== null && max !== null && min > max) {
      toast.error(t('Settings.PriceLists.Lines.Errors.MinExceedsMax'));
      return;
    }
    const discount = parseNumber(draft.discountPercent);
    if (discount !== null && (discount < 0 || discount > 100)) {
      toast.error(t('Settings.PriceLists.Lines.Errors.DiscountRange'));
      return;
    }

    try {
      if (draft.id) {
        await updateMutation.mutateAsync({
          priceListId: priceList.id,
          id: draft.id,
          price,
          minQuantity: min,
          maxQuantity: max,
          discountPercent: discount,
        });
        toast.success(t('Settings.PriceLists.Lines.Updated'));
      } else {
        await addMutation.mutateAsync({
          priceListId: priceList.id,
          productId: draft.productId,
          price,
          minQuantity: min,
          maxQuantity: max,
          discountPercent: discount,
        });
        toast.success(t('Settings.PriceLists.Lines.Added'));
      }
      setDraft(null);
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleRemove = async (item: PriceListItem) => {
    const ok = await confirm({
      title: t('Settings.PriceLists.Lines.RemoveTitle'),
      message: t('Settings.PriceLists.Lines.RemoveMessage'),
      confirmLabel: t('Common.Delete'),
    });
    if (!ok) return;
    try {
      await removeMutation.mutateAsync(item.id);
      toast.success(t('Settings.PriceLists.Lines.Removed'));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
          {t('Settings.PriceLists.Lines.Title')}
        </h3>
        {draft === null && (
          <button
            type="button"
            onClick={beginAdd}
            className="inline-flex items-center gap-1 rounded bg-indigo-600 px-2.5 py-1 text-xs font-medium text-white hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600"
          >
            <Plus size={12} /> {t('Settings.PriceLists.Lines.Add')}
          </button>
        )}
      </div>

      <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
        <table className="min-w-full text-xs">
          <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800 dark:text-slate-400">
            <tr>
              <th className="px-2 py-1.5 font-medium">{t('Settings.PriceLists.Lines.Product')}</th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.PriceLists.Lines.MinQuantity')}
              </th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.PriceLists.Lines.MaxQuantity')}
              </th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.PriceLists.Lines.Price')}
              </th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.PriceLists.Lines.DiscountPercent')}
              </th>
              <th className="px-2 py-1.5"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {items.map((item) => {
              const isEditing = draft?.id === item.id;
              if (isEditing && draft) {
                return (
                  <DraftRow
                    key={item.id}
                    draft={draft}
                    products={products}
                    onChange={setDraft}
                    onSave={saveDraft}
                    onCancel={cancel}
                    isProductLocked
                  />
                );
              }
              return (
                <tr key={item.id} className="bg-white dark:bg-slate-900">
                  <td className="px-2 py-1.5 text-slate-700 dark:text-slate-200">
                    {productNameById.get(item.productId) ?? item.productId}
                  </td>
                  <td className="px-2 py-1.5 text-right text-slate-600 dark:text-slate-300">
                    {item.minQuantity ?? '—'}
                  </td>
                  <td className="px-2 py-1.5 text-right text-slate-600 dark:text-slate-300">
                    {item.maxQuantity ?? '—'}
                  </td>
                  <td className="px-2 py-1.5 text-right text-slate-800 dark:text-slate-100">
                    {item.price}
                  </td>
                  <td className="px-2 py-1.5 text-right text-slate-600 dark:text-slate-300">
                    {item.discountPercent ?? '—'}
                  </td>
                  <td className="px-2 py-1.5 text-right">
                    <button
                      type="button"
                      onClick={() => beginEdit(item)}
                      className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
                      aria-label={t('Common.Edit')}
                    >
                      <Pencil size={12} />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleRemove(item)}
                      className="rounded p-1 text-red-500 hover:bg-red-50 dark:hover:bg-red-900/30"
                      aria-label={t('Common.Delete')}
                    >
                      <Trash2 size={12} />
                    </button>
                  </td>
                </tr>
              );
            })}
            {draft && !draft.id && (
              <DraftRow
                draft={draft}
                products={products}
                onChange={setDraft}
                onSave={saveDraft}
                onCancel={cancel}
              />
            )}
            {items.length === 0 && draft === null && (
              <tr>
                <td
                  colSpan={6}
                  className="px-2 py-4 text-center text-slate-400 dark:text-slate-500"
                >
                  {t('Settings.PriceLists.Lines.Empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

interface DraftRowProps {
  draft: RowDraft;
  products: { id: string; sku: string; name: string }[];
  onChange: (draft: RowDraft) => void;
  onSave: () => void;
  onCancel: () => void;
  isProductLocked?: boolean;
}

const DraftRow = ({
  draft,
  products,
  onChange,
  onSave,
  onCancel,
  isProductLocked,
}: DraftRowProps) => {
  const { t } = useTranslation();
  return (
    <tr className="bg-indigo-50/40 dark:bg-indigo-900/10">
      <td className="px-2 py-1.5">
        <select
          value={draft.productId}
          disabled={isProductLocked}
          onChange={(e) => onChange({ ...draft, productId: e.target.value })}
          className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-xs dark:border-slate-700 dark:bg-slate-900"
        >
          <option value="">{t('Settings.PriceLists.Lines.SelectProduct')}</option>
          {products.map((p) => (
            <option key={p.id} value={p.id}>
              {p.sku} — {p.name}
            </option>
          ))}
        </select>
      </td>
      <td className="px-2 py-1.5">
        <input
          type="number"
          inputMode="decimal"
          value={draft.minQuantity}
          onChange={(e) => onChange({ ...draft, minQuantity: e.target.value })}
          className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-right text-xs dark:border-slate-700 dark:bg-slate-900"
        />
      </td>
      <td className="px-2 py-1.5">
        <input
          type="number"
          inputMode="decimal"
          value={draft.maxQuantity}
          onChange={(e) => onChange({ ...draft, maxQuantity: e.target.value })}
          className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-right text-xs dark:border-slate-700 dark:bg-slate-900"
        />
      </td>
      <td className="px-2 py-1.5">
        <input
          type="number"
          inputMode="decimal"
          value={draft.price}
          onChange={(e) => onChange({ ...draft, price: e.target.value })}
          className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-right text-xs dark:border-slate-700 dark:bg-slate-900"
        />
      </td>
      <td className="px-2 py-1.5">
        <input
          type="number"
          inputMode="decimal"
          value={draft.discountPercent}
          onChange={(e) => onChange({ ...draft, discountPercent: e.target.value })}
          className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-right text-xs dark:border-slate-700 dark:bg-slate-900"
        />
      </td>
      <td className="px-2 py-1.5 text-right">
        <button
          type="button"
          onClick={onSave}
          className="mr-1 rounded bg-indigo-600 px-2 py-0.5 text-[11px] font-medium text-white hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600"
        >
          {t('Common.Save')}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="rounded border border-slate-300 px-2 py-0.5 text-[11px] text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
        >
          {t('Common.Cancel')}
        </button>
      </td>
    </tr>
  );
};
