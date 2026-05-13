import { useEffect, useMemo } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, Trash2, X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { orderSchema, type OrderFormValues } from '../model/orderSchema';
import { ORDER_STATUSES, type Order } from '../model/order.types';
import { useCreateOrder, useUpdateOrder } from '../hooks/useOrderQueries';

interface Props {
  open: boolean;
  order: Order | null;
  onClose: () => void;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const emptyValues: OrderFormValues = {
  orderNumber: '',
  customerId: '',
  orderDate: todayIso(),
  status: 'Draft',
  currency: 'USD',
  notes: '',
  lines: [{ productId: '', quantity: 1, unitPrice: 0 }],
};

export const OrderFormModal = ({ open, order, onClose }: Props) => {
  const { t, i18n } = useTranslation();
  const createMutation = useCreateOrder();
  const updateMutation = useUpdateOrder();
  const isEdit = order !== null;
  const isDraft = !isEdit || order?.status === 'Draft';

  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });

  const customers = customersQuery.data?.data?.items ?? [];
  const products = productsQuery.data?.data?.items ?? [];

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(orderSchema),
    defaultValues: emptyValues,
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  useEffect(() => {
    if (!open) return;
    if (order) {
      reset({
        orderNumber: order.orderNumber,
        customerId: order.customerId,
        orderDate: order.orderDate.slice(0, 10),
        status: order.status,
        currency: order.currency,
        notes: order.notes ?? '',
        lines: order.lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
        })),
      });
    } else {
      reset(emptyValues);
    }
  }, [open, order, reset]);

  const watchedLines = useWatch({ control, name: 'lines' });
  const watchedCurrency = useWatch({ control, name: 'currency' });

  const total = useMemo(() => {
    return (watchedLines ?? []).reduce(
      (sum, l) => sum + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0),
      0,
    );
  }, [watchedLines]);

  const formatTotal = (value: number, currency: string) => {
    try {
      return new Intl.NumberFormat(i18n.language, { style: 'currency', currency }).format(value);
    } catch {
      return `${value.toFixed(2)} ${currency}`;
    }
  };

  const handleProductSelect = (index: number, productId: string) => {
    setValue(`lines.${index}.productId`, productId, { shouldValidate: true });
    const product = products.find((p) => p.id === productId);
    if (product) {
      setValue(`lines.${index}.unitPrice`, product.price);
    }
  };

  const onSubmit = handleSubmit((values) => {
    const payload = {
      orderNumber: values.orderNumber,
      customerId: values.customerId,
      orderDate: new Date(values.orderDate).toISOString(),
      currency: values.currency.toUpperCase(),
      notes: values.notes || null,
      lines: values.lines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
      })),
    };

    if (isEdit && order) {
      updateMutation.mutate(
        { ...payload, id: order.id, status: values.status },
        {
          onSuccess: (response) => {
            if (response.isSuccess) {
              toast.success(t('orders.toast.updated'));
              onClose();
              return;
            }
            toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
          },
          onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
        },
      );
      return;
    }

    createMutation.mutate(payload, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('orders.toast.created'));
          onClose();
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  });

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (!open) return null;

  const isBusy = isSubmitting || createMutation.isPending || updateMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="w-full max-w-3xl max-h-[90vh] overflow-y-auto rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-200 bg-white px-5 py-3 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {isEdit ? t('orders.modal.editTitle') : t('orders.modal.createTitle')}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.cancel')}
          >
            <X size={18} />
          </button>
        </div>

        <form onSubmit={onSubmit} noValidate className="space-y-4 px-5 py-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Input
              label={t('orders.fields.orderNumber')}
              placeholder="ORD-2026-0001"
              disabled={!isDraft}
              error={translateError(errors.orderNumber?.message)}
              {...register('orderNumber')}
            />
            <Input
              label={t('orders.fields.orderDate')}
              type="date"
              disabled={!isDraft}
              error={translateError(errors.orderDate?.message)}
              {...register('orderDate')}
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('orders.fields.customer')}
            </label>
            <select
              disabled={!isDraft}
              className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              {...register('customerId')}
            >
              <option value="">{t('orders.fields.customerPlaceholder')}</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            {errors.customerId?.message && (
              <span className="mt-1 block text-xs text-red-500">
                {translateError(errors.customerId.message)}
              </span>
            )}
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('orders.fields.status')}
              </label>
              <select
                disabled={!isEdit}
                className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                {...register('status')}
              >
                {ORDER_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {t(`orders.status.${s}` as never)}
                  </option>
                ))}
              </select>
            </div>
            <Input
              label={t('orders.fields.currency')}
              placeholder="USD"
              error={translateError(errors.currency?.message)}
              {...register('currency')}
            />
            <div className="flex items-end">
              <div className="w-full rounded border border-slate-200 bg-slate-50 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800/50">
                <div className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('orders.fields.total')}
                </div>
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {formatTotal(total, watchedCurrency || 'USD')}
                </div>
              </div>
            </div>
          </div>

          <div>
            <div className="mb-2 flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('orders.fields.lines')}
              </span>
              <button
                type="button"
                disabled={!isDraft}
                onClick={() => append({ productId: '', quantity: 1, unitPrice: 0 })}
                className="inline-flex items-center gap-1 rounded bg-indigo-50 px-2 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-100 disabled:opacity-50 dark:bg-indigo-500/10 dark:text-indigo-300 dark:hover:bg-indigo-500/20"
              >
                <Plus size={12} />
                {t('orders.lines.add')}
              </button>
            </div>

            {errors.lines?.message && (
              <div className="mb-2 text-xs text-red-500">
                {translateError(errors.lines.message)}
              </div>
            )}

            <div className="space-y-2">
              {fields.map((field, index) => (
                <div
                  key={field.id}
                  className="grid grid-cols-12 gap-2 rounded border border-slate-200 bg-slate-50/50 p-2 dark:border-slate-800 dark:bg-slate-800/30"
                >
                  <div className="col-span-12 sm:col-span-6">
                    <select
                      disabled={!isDraft}
                      className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                      value={watchedLines?.[index]?.productId ?? ''}
                      onChange={(e) => handleProductSelect(index, e.target.value)}
                    >
                      <option value="">{t('orders.lines.productPlaceholder')}</option>
                      {products.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.sku} — {p.name} ({p.stockQuantity} {p.unit})
                        </option>
                      ))}
                    </select>
                    {errors.lines?.[index]?.productId?.message && (
                      <span className="mt-1 block text-xs text-red-500">
                        {translateError(errors.lines[index]?.productId?.message)}
                      </span>
                    )}
                  </div>
                  <div className="col-span-5 sm:col-span-2">
                    <Input
                      type="number"
                      step="0.0001"
                      placeholder={t('orders.lines.quantity')}
                      disabled={!isDraft}
                      error={translateError(errors.lines?.[index]?.quantity?.message)}
                      {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
                    />
                  </div>
                  <div className="col-span-5 sm:col-span-3">
                    <Input
                      type="number"
                      step="0.0001"
                      placeholder={t('orders.lines.unitPrice')}
                      disabled={!isDraft}
                      error={translateError(errors.lines?.[index]?.unitPrice?.message)}
                      {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })}
                    />
                  </div>
                  <div className="col-span-2 sm:col-span-1 flex items-start justify-end">
                    <button
                      type="button"
                      onClick={() => remove(index)}
                      disabled={!isDraft || fields.length === 1}
                      className="rounded p-2 text-slate-500 hover:bg-red-50 hover:text-red-600 disabled:opacity-30 dark:text-slate-400 dark:hover:bg-red-500/10"
                      aria-label={t('common.delete')}
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('orders.fields.notes')}
            </label>
            <textarea
              rows={2}
              placeholder={t('orders.fields.notesPlaceholder')}
              className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder-slate-500"
              {...register('notes')}
            />
          </div>

          <div className="sticky bottom-0 flex justify-end gap-2 border-t border-slate-200 bg-white pt-3 dark:border-slate-800 dark:bg-slate-900">
            <button
              type="button"
              onClick={onClose}
              className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('common.cancel')}
            </button>
            <Button type="submit" isLoading={isBusy}>
              {t('common.save')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
