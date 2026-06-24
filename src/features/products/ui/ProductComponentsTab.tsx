import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Check, Layers, Pencil, Plus, Trash2, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useAddProductComponent,
  useProductComponentsQuery,
  useProductsQuery,
  useRemoveProductComponent,
  useUpdateProductComponent,
} from '@/features/products/hooks/useProductQueries';
import {
  emptyProductComponentForm,
  productComponentSchema,
  type ProductComponentFormValues,
} from '@/features/products/model/productComponentSchema';
import type { ProductComponent } from '@/features/products/model/product.types';

interface Props {
  productId: string;
}

export const ProductComponentsTab = ({ productId }: Props) => {
  const { t, i18n } = useTranslation();
  const componentsQuery = useProductComponentsQuery(productId);
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const addMutation = useAddProductComponent();
  const updateMutation = useUpdateProductComponent();
  const removeMutation = useRemoveProductComponent();
  const confirm = useConfirm();

  const [editing, setEditing] = useState<string | 'new' | null>(null);

  const components = useMemo(() => componentsQuery.data?.data ?? [], [componentsQuery.data]);
  const productCatalog = useMemo(() => productsQuery.data?.data?.items ?? [], [productsQuery.data]);

  const usedComponentIds = useMemo(
    () => new Set(components.map((c) => c.componentProductId)),
    [components],
  );
  const availableProducts = useMemo(
    () =>
      productCatalog.filter(
        (p) =>
          p.id !== productId && (editing && editing !== 'new' ? true : !usedComponentIds.has(p.id)),
      ),
    [productCatalog, productId, usedComponentIds, editing],
  );

  const initial =
    editing && editing !== 'new' ? (components.find((c) => c.id === editing) ?? null) : null;

  const handleSubmit = (values: ProductComponentFormValues) => {
    const onComplete = (msg: string) => {
      toast.success(msg);
      setEditing(null);
    };

    if (editing === 'new') {
      addMutation.mutate(
        {
          parentProductId: productId,
          componentProductId: values.componentProductId,
          quantity: values.quantity,
          notes: values.notes || null,
        },
        {
          onSuccess: () => onComplete(t('products.detail.components.toast.added')),
          onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
        },
      );
    } else if (editing) {
      updateMutation.mutate(
        {
          parentProductId: productId,
          id: editing,
          quantity: values.quantity,
          notes: values.notes || null,
        },
        {
          onSuccess: () => onComplete(t('products.detail.components.toast.updated')),
          onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
        },
      );
    }
  };

  const remove = async (c: ProductComponent) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('products.detail.components.confirmDelete', { name: c.componentName }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;
    removeMutation.mutate(
      { parentProductId: productId, id: c.id },
      {
        onSuccess: () => toast.success(t('products.detail.components.toast.removed')),
        onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
      },
    );
  };

  return (
    <div className="space-y-3">
      <div className="rounded-lg border border-primary-200 bg-primary-50/40 px-3 py-2 text-[11px] text-primary-700 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300">
        {t('products.detail.components.intro')}
      </div>

      {editing === null && (
        <button
          type="button"
          onClick={() => setEditing('new')}
          disabled={availableProducts.length === 0 && components.length === 0}
          className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-dashed border-slate-300 bg-slate-50/50 px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-800/30 dark:text-slate-300 dark:hover:bg-slate-800"
        >
          <Plus size={14} />
          {t('products.detail.components.addNew')}
        </button>
      )}

      {editing !== null && (
        <ComponentForm
          key={editing}
          mode={editing === 'new' ? 'new' : 'edit'}
          initial={
            initial
              ? {
                  componentProductId: initial.componentProductId,
                  quantity: initial.quantity,
                  notes: initial.notes ?? '',
                }
              : emptyProductComponentForm
          }
          availableProducts={availableProducts.map((p) => ({
            id: p.id,
            label: `${p.sku} — ${p.name}`,
          }))}
          lockedComponent={initial ? `${initial.componentSku} — ${initial.componentName}` : null}
          onSubmit={handleSubmit}
          onCancel={() => setEditing(null)}
          saving={addMutation.isPending || updateMutation.isPending}
        />
      )}

      {componentsQuery.isPending && components.length === 0 ? (
        <div className="text-sm text-slate-500">{t('common.loading')}</div>
      ) : components.length === 0 && editing === null ? (
        <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
          {t('products.detail.components.empty')}
        </div>
      ) : (
        <ul className="space-y-2">
          {components.map((c) => (
            <li
              key={c.id}
              className="flex items-center justify-between gap-2 rounded-lg border border-slate-200 p-3 dark:border-slate-800"
            >
              <div className="min-w-0">
                <div className="flex items-center gap-1.5">
                  <Layers size={12} className="text-slate-500" />
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {c.componentName}
                  </span>
                  <span className="font-mono text-[10px] text-slate-500">{c.componentSku}</span>
                </div>
                {c.notes && (
                  <div className="mt-0.5 text-[10px] italic text-slate-500">{c.notes}</div>
                )}
              </div>
              <div className="flex shrink-0 items-center gap-2">
                <span className="text-sm font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                  {new Intl.NumberFormat(i18n.language).format(c.quantity)} {c.componentUnit}
                </span>
                <button
                  type="button"
                  onClick={() => setEditing(c.id)}
                  className="rounded p-1 text-slate-500 hover:bg-slate-100 hover:text-primary-600 dark:hover:bg-slate-800 dark:hover:text-primary-400"
                  aria-label={t('common.edit')}
                >
                  <Pencil size={12} />
                </button>
                <button
                  type="button"
                  onClick={() => remove(c)}
                  className="rounded p-1 text-slate-500 hover:bg-danger-50 hover:text-danger-600 dark:hover:bg-danger-500/10 dark:hover:text-danger-400"
                  aria-label={t('common.delete')}
                >
                  <Trash2 size={12} />
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

const ComponentForm = ({
  mode,
  initial,
  availableProducts,
  lockedComponent,
  onSubmit,
  onCancel,
  saving,
}: {
  mode: 'new' | 'edit';
  initial: ProductComponentFormValues;
  availableProducts: { id: string; label: string }[];
  lockedComponent: string | null;
  onSubmit: (values: ProductComponentFormValues) => void;
  onCancel: () => void;
  saving: boolean;
}) => {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProductComponentFormValues>({
    resolver: zodResolver(productComponentSchema),
    defaultValues: initial,
  });

  useEffect(() => {
    reset(initial);
  }, [initial, reset]);

  const fieldError = (key?: unknown): string | undefined =>
    typeof key === 'string' ? t(key, { defaultValue: key }) : undefined;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-2 rounded-lg border border-primary-200 bg-primary-50/30 p-3 dark:border-primary-500/30 dark:bg-primary-500/5"
    >
      <Field
        label={t('products.detail.components.fields.component')}
        error={fieldError(errors.componentProductId?.message)}
      >
        {mode === 'edit' ? (
          <input
            value={lockedComponent ?? ''}
            readOnly
            className="w-full rounded border border-slate-200 bg-slate-50 px-2 py-1 text-xs text-slate-700 dark:border-slate-700 dark:bg-slate-800/50 dark:text-slate-200"
          />
        ) : (
          <select
            {...register('componentProductId')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            <option value="">{t('products.detail.components.fields.componentPlaceholder')}</option>
            {availableProducts.map((p) => (
              <option key={p.id} value={p.id}>
                {p.label}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field
        label={t('products.detail.components.fields.quantity')}
        error={fieldError(errors.quantity?.message)}
      >
        <input
          type="number"
          step="0.0001"
          min="0"
          {...register('quantity', { valueAsNumber: true })}
          className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </Field>
      <Field
        label={t('products.detail.components.fields.notes')}
        error={fieldError(errors.notes?.message)}
      >
        <input
          {...register('notes')}
          className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </Field>
      <div className="flex gap-2 pt-1">
        <button
          type="submit"
          disabled={saving}
          className="inline-flex flex-1 items-center justify-center gap-1.5 rounded bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-700 disabled:opacity-50"
        >
          <Check size={12} />
          {t('common.save')}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex items-center justify-center gap-1.5 rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <X size={12} />
          {t('common.cancel')}
        </button>
      </div>
    </form>
  );
};

const Field = ({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) => (
  <label className="block">
    <span className="mb-0.5 block text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    {children}
    {error && <span className="mt-0.5 block text-[10px] text-danger-500">{error}</span>}
  </label>
);
