import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import {
  useCreateProductVariant,
  useDeleteProductVariant,
  useProductVariantsQuery,
  useUpdateProductVariant,
} from '@/features/products/hooks/useProductVariants';
import type { ProductVariant } from '@/features/products/api/productVariantsApi';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Textarea } from '@/shared/ui/Textarea/Textarea';

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
          className="inline-flex items-center gap-1.5 rounded-[5px] bg-primary-600 text-white text-[11px] font-semibold px-2.5 py-1.5 hover:bg-primary-500"
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
                          ? 'inline-block rounded-[3px] bg-success-100 text-success-700 px-1.5 py-0.5 text-[10px]'
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
                        className="p-1 rounded-[3px] hover:bg-danger-50 dark:hover:bg-danger-500/10 text-danger-500 disabled:opacity-40"
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

      <Modal
        open={modalOpen}
        title={
          form.id
            ? t('Products.Variants.editTitle', { defaultValue: 'Edit variant' })
            : t('Products.Variants.createTitle', { defaultValue: 'New variant' })
        }
        icon={form.id ? <Pencil size={18} /> : <Plus size={18} />}
        onClose={closeModal}
        size="md"
        footer={
          <>
            <Button type="button" variant="ghost" onClick={closeModal}>
              {t('Products.Variants.cancel', { defaultValue: 'Cancel' })}
            </Button>
            <Button
              type="submit"
              form="product-variant-form"
              isLoading={createMutation.isPending || updateMutation.isPending}
            >
              {t('Products.Variants.save', { defaultValue: 'Save' })}
            </Button>
          </>
        }
      >
        <form id="product-variant-form" onSubmit={submit} className="space-y-3">
          <Input
            label={t('Products.Variants.sku', { defaultValue: 'SKU' })}
            type="text"
            required
            value={form.sku}
            onChange={(e) => setForm((f) => ({ ...f, sku: e.target.value }))}
          />
          <Input
            label={t('Products.Variants.barcode', { defaultValue: 'Barcode' })}
            type="text"
            value={form.barcode}
            onChange={(e) => setForm((f) => ({ ...f, barcode: e.target.value }))}
          />
          <div>
            <Textarea
              label={t('Products.Variants.attributes', { defaultValue: 'Attributes (JSON)' })}
              value={form.variantAttributesJson}
              onChange={(e) => setForm((f) => ({ ...f, variantAttributesJson: e.target.value }))}
              rows={3}
              className="font-mono"
            />
            <span className="mt-1 block text-[10px] text-slate-400 dark:text-slate-500">
              {t('Products.Variants.attributesHint', {
                defaultValue: 'JSON object, e.g. {"color":"red","size":"L"}',
              })}
            </span>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Input
              label={t('Products.Variants.priceOverride', { defaultValue: 'Price override' })}
              type="number"
              step="0.0001"
              min="0"
              value={form.priceOverride}
              onChange={(e) => setForm((f) => ({ ...f, priceOverride: e.target.value }))}
            />
            <Input
              label={t('Products.Variants.stock', { defaultValue: 'Stock' })}
              type="number"
              step="0.0001"
              min="0"
              value={form.stockQuantity}
              disabled={!!form.id}
              onChange={(e) => setForm((f) => ({ ...f, stockQuantity: e.target.value }))}
            />
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
        </form>
      </Modal>
    </section>
  );
};

export default ProductVariantsTab;
