import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Trash2, X } from 'lucide-react';
import { toast } from 'sonner';
import {
  useCreateProductVariant,
  useDeleteProductVariant,
  useProductVariantsQuery,
  useUpdateProductVariant,
} from '@/features/products/hooks/useProductVariants';
import type { ProductVariant } from '@/features/products/api/productVariantsApi';

interface ProductVariantsTabProps {
  productId: string;
}

interface VariantFormState {
  id: string | null;
  sku: string;
  barcode: string;
  variantAttributesJson: string;
  priceOverride: string;
  stockQuantity: string;
  isActive: boolean;
}

const emptyForm = (): VariantFormState => ({
  id: null,
  sku: '',
  barcode: '',
  variantAttributesJson: '{}',
  priceOverride: '',
  stockQuantity: '0',
  isActive: true,
});

const parseAttributes = (raw: string): boolean => {
  try {
    const parsed = JSON.parse(raw);
    return !!parsed && typeof parsed === 'object' && !Array.isArray(parsed);
  } catch {
    return false;
  }
};

export const ProductVariantsTab = ({ productId }: ProductVariantsTabProps) => {
  const { t } = useTranslation();
  const query = useProductVariantsQuery(productId);
  const createMutation = useCreateProductVariant(productId);
  const updateMutation = useUpdateProductVariant(productId);
  const deleteMutation = useDeleteProductVariant(productId);

  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState<VariantFormState>(emptyForm());
  const [pendingId, setPendingId] = useState<string | null>(null);

  const variants = useMemo(() => query.data ?? [], [query.data]);

  const openCreate = () => {
    setForm(emptyForm());
    setModalOpen(true);
  };

  const openEdit = (variant: ProductVariant) => {
    setForm({
      id: variant.id,
      sku: variant.sku,
      barcode: variant.barcode ?? '',
      variantAttributesJson: variant.variantAttributesJson || '{}',
      priceOverride: variant.priceOverride !== null ? String(variant.priceOverride) : '',
      stockQuantity: String(variant.stockQuantity),
      isActive: variant.isActive,
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setForm(emptyForm());
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const sku = form.sku.trim();
    if (!sku) {
      toast.error(t('Products.Variants.skuRequired', { defaultValue: 'SKU is required.' }));
      return;
    }
    if (!parseAttributes(form.variantAttributesJson)) {
      toast.error(
        t('Products.Variants.attributesInvalid', {
          defaultValue: 'Attributes must be a JSON object.',
        }),
      );
      return;
    }
    const priceOverride = form.priceOverride.trim() === '' ? null : Number(form.priceOverride);
    const stockQuantity = Number(form.stockQuantity);

    try {
      if (form.id) {
        await updateMutation.mutateAsync({
          variantId: form.id,
          payload: {
            sku,
            barcode: form.barcode.trim() || null,
            variantAttributesJson: form.variantAttributesJson,
            priceOverride,
            isActive: form.isActive,
          },
        });
      } else {
        await createMutation.mutateAsync({
          sku,
          barcode: form.barcode.trim() || null,
          variantAttributesJson: form.variantAttributesJson,
          priceOverride,
          stockQuantity: Number.isFinite(stockQuantity) ? stockQuantity : 0,
          isActive: form.isActive,
        });
      }
      closeModal();
    } catch {
      toast.error(t('Products.Variants.saveFailed', { defaultValue: 'Save failed' }));
    }
  };

  const confirmDelete = async (variant: ProductVariant) => {
    const confirmed = window.confirm(
      t('Products.Variants.deleteConfirm', { defaultValue: 'Delete this variant?' }),
    );
    if (!confirmed) return;
    setPendingId(variant.id);
    try {
      await deleteMutation.mutateAsync(variant.id);
    } finally {
      setPendingId(null);
    }
  };

  return (
    <section className="space-y-3" data-testid="product-variants-tab">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xs font-semibold text-slate-800 dark:text-slate-100">
            {t('Products.Variants.title', { defaultValue: 'Variants' })}
          </h3>
          <p className="text-[11px] text-slate-500 dark:text-slate-400">
            {t('Products.Variants.subtitle', {
              defaultValue: 'Manage SKUs that share this product (e.g. color/size).',
            })}
          </p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex items-center gap-1.5 rounded-[5px] bg-indigo-600 text-white text-[11px] font-semibold px-2.5 py-1.5 hover:bg-indigo-500"
        >
          <Plus className="h-3.5 w-3.5" />
          {t('Products.Variants.add', { defaultValue: 'Add variant' })}
        </button>
      </div>

      {query.isLoading && (
        <p className="text-[11px] text-slate-500">
          {t('Products.Variants.loading', { defaultValue: 'Loading variants…' })}
        </p>
      )}

      {!query.isLoading && variants.length === 0 && (
        <div className="rounded-[5px] border border-dashed border-slate-200 dark:border-slate-700 py-8 text-center text-[11px] text-slate-400">
          {t('Products.Variants.empty', { defaultValue: 'No variants yet.' })}
        </div>
      )}

      {variants.length > 0 && (
        <div className="overflow-x-auto rounded-[5px] border border-slate-200 dark:border-slate-700">
          <table className="min-w-full text-[11px]">
            <thead className="bg-slate-50 dark:bg-slate-800/50 text-slate-600 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-start font-semibold">
                  {t('Products.Variants.sku', { defaultValue: 'SKU' })}
                </th>
                <th className="px-3 py-2 text-start font-semibold">
                  {t('Products.Variants.attributes', { defaultValue: 'Attributes (JSON)' })}
                </th>
                <th className="px-3 py-2 text-end font-semibold">
                  {t('Products.Variants.priceOverride', { defaultValue: 'Price override' })}
                </th>
                <th className="px-3 py-2 text-end font-semibold">
                  {t('Products.Variants.stock', { defaultValue: 'Stock' })}
                </th>
                <th className="px-3 py-2 text-center font-semibold">
                  {t('Products.Variants.active', { defaultValue: 'Active' })}
                </th>
                <th className="px-3 py-2 text-end font-semibold">
                  {t('Products.Variants.actions', { defaultValue: 'Actions' })}
                </th>
              </tr>
            </thead>
            <tbody>
              {variants.map((variant) => (
                <tr
                  key={variant.id}
                  data-testid={`product-variant-${variant.id}`}
                  className="border-t border-slate-100 dark:border-slate-800"
                >
                  <td className="px-3 py-2 font-mono text-slate-800 dark:text-slate-100">
                    {variant.sku}
                    {variant.barcode && (
                      <div className="text-[10px] text-slate-400">{variant.barcode}</div>
                    )}
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                    <code className="text-[10px] break-all">{variant.variantAttributesJson}</code>
                  </td>
                  <td className="px-3 py-2 text-end text-slate-700 dark:text-slate-200">
                    {variant.priceOverride !== null ? variant.priceOverride.toFixed(2) : '—'}
                  </td>
                  <td className="px-3 py-2 text-end text-slate-700 dark:text-slate-200">
                    {variant.stockQuantity}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <span
                      className={
                        variant.isActive
                          ? 'inline-block rounded-[3px] bg-emerald-100 text-emerald-700 px-1.5 py-0.5 text-[10px]'
                          : 'inline-block rounded-[3px] bg-slate-100 text-slate-500 px-1.5 py-0.5 text-[10px]'
                      }
                    >
                      {variant.isActive ? '✓' : '—'}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-end">
                    <div className="inline-flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => openEdit(variant)}
                        aria-label={t('Products.Variants.edit', { defaultValue: 'Edit' })}
                        className="p-1 rounded-[3px] hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={() => void confirmDelete(variant)}
                        disabled={pendingId === variant.id}
                        aria-label={t('Products.Variants.delete', { defaultValue: 'Delete' })}
                        className="p-1 rounded-[3px] hover:bg-red-50 dark:hover:bg-red-500/10 text-red-500 disabled:opacity-40"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <form
            onSubmit={submit}
            className="w-full max-w-md rounded-[6px] bg-white dark:bg-slate-900 shadow-lg p-4 space-y-3"
          >
            <div className="flex items-center justify-between">
              <h4 className="text-xs font-semibold text-slate-800 dark:text-slate-100">
                {form.id
                  ? t('Products.Variants.editTitle', { defaultValue: 'Edit variant' })
                  : t('Products.Variants.createTitle', { defaultValue: 'New variant' })}
              </h4>
              <button
                type="button"
                onClick={closeModal}
                className="p-1 rounded-[3px] hover:bg-slate-100 dark:hover:bg-slate-800"
                aria-label={t('Products.Variants.cancel', { defaultValue: 'Cancel' })}
              >
                <X className="h-4 w-4" />
              </button>
            </div>
            <div className="space-y-2">
              <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-200">
                {t('Products.Variants.sku', { defaultValue: 'SKU' })}
                <input
                  type="text"
                  required
                  value={form.sku}
                  onChange={(e) => setForm((f) => ({ ...f, sku: e.target.value }))}
                  className="mt-1 w-full text-[11px] px-2 py-1.5 rounded-[3px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
                />
              </label>
              <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-200">
                {t('Products.Variants.barcode', { defaultValue: 'Barcode' })}
                <input
                  type="text"
                  value={form.barcode}
                  onChange={(e) => setForm((f) => ({ ...f, barcode: e.target.value }))}
                  className="mt-1 w-full text-[11px] px-2 py-1.5 rounded-[3px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
                />
              </label>
              <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-200">
                {t('Products.Variants.attributes', { defaultValue: 'Attributes (JSON)' })}
                <textarea
                  value={form.variantAttributesJson}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, variantAttributesJson: e.target.value }))
                  }
                  rows={3}
                  className="mt-1 w-full text-[11px] font-mono px-2 py-1.5 rounded-[3px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
                />
                <span className="block text-[10px] text-slate-400 mt-1">
                  {t('Products.Variants.attributesHint', {
                    defaultValue: 'JSON object, e.g. {"color":"red","size":"L"}',
                  })}
                </span>
              </label>
              <div className="grid grid-cols-2 gap-2">
                <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-200">
                  {t('Products.Variants.priceOverride', { defaultValue: 'Price override' })}
                  <input
                    type="number"
                    step="0.0001"
                    min="0"
                    value={form.priceOverride}
                    onChange={(e) => setForm((f) => ({ ...f, priceOverride: e.target.value }))}
                    className="mt-1 w-full text-[11px] px-2 py-1.5 rounded-[3px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
                  />
                </label>
                <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-200">
                  {t('Products.Variants.stock', { defaultValue: 'Stock' })}
                  <input
                    type="number"
                    step="0.0001"
                    min="0"
                    value={form.stockQuantity}
                    disabled={!!form.id}
                    onChange={(e) => setForm((f) => ({ ...f, stockQuantity: e.target.value }))}
                    className="mt-1 w-full text-[11px] px-2 py-1.5 rounded-[3px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 disabled:bg-slate-50 dark:disabled:bg-slate-800"
                  />
                </label>
              </div>
              <label className="inline-flex items-center gap-2 text-[11px] text-slate-700 dark:text-slate-200">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                  className="h-3.5 w-3.5 rounded-[2px] border-slate-300"
                />
                {t('Products.Variants.active', { defaultValue: 'Active' })}
              </label>
            </div>
            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={closeModal}
                className="px-3 py-1.5 text-[11px] rounded-[3px] bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-200 hover:bg-slate-200"
              >
                {t('Products.Variants.cancel', { defaultValue: 'Cancel' })}
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
                className="px-3 py-1.5 text-[11px] rounded-[3px] bg-indigo-600 text-white hover:bg-indigo-500 disabled:opacity-60"
              >
                {t('Products.Variants.save', { defaultValue: 'Save' })}
              </button>
            </div>
          </form>
        </div>
      )}
    </section>
  );
};

export default ProductVariantsTab;
