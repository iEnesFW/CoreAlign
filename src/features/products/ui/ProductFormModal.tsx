import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { productSchema, type ProductFormValues } from '../model/productSchema';
import type { Product } from '../model/product.types';
import { useCreateProduct, useUpdateProduct } from '../hooks/useProductQueries';

interface Props {
  open: boolean;
  product: Product | null;
  onClose: () => void;
}

const emptyValues: ProductFormValues = {
  sku: '',
  name: '',
  description: '',
  unit: 'pcs',
  price: 0,
  currency: 'USD',
  stockQuantity: 0,
  isActive: true,
};

export const ProductFormModal = ({ open, product, onClose }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateProduct();
  const updateMutation = useUpdateProduct();
  const isEdit = product !== null;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (!open) return;
    if (product) {
      reset({
        sku: product.sku,
        name: product.name,
        description: product.description ?? '',
        unit: product.unit,
        price: product.price,
        currency: product.currency,
        stockQuantity: product.stockQuantity,
        isActive: product.isActive,
      });
    } else {
      reset(emptyValues);
    }
  }, [open, product, reset]);

  const onSubmit = handleSubmit((values) => {
    const payload = {
      sku: values.sku,
      name: values.name,
      description: values.description || null,
      unit: values.unit,
      price: values.price,
      currency: values.currency.toUpperCase(),
      stockQuantity: values.stockQuantity,
    };

    if (isEdit && product) {
      updateMutation.mutate(
        {
          ...payload,
          id: product.id,
          shortDescription: product.shortDescription,
          barcode: product.barcode,
          mpn: product.mpn,
          slug: product.slug,
          brandId: product.brandId,
          categoryId: product.categoryId,
          parentProductId: product.parentProductId,
          variantAttributesJson: product.variantAttributesJson,
          tagsJson: product.tagsJson,
          baseUomId: product.baseUomId,
          purchaseUomId: product.purchaseUomId,
          salesUomId: product.salesUomId,
          listPrice: product.listPrice || values.price,
          minSellingPrice: product.minSellingPrice,
          standardCost: product.standardCost,
          taxRateId: product.taxRateId,
          isPriceTaxInclusive: product.isPriceTaxInclusive,
          isStockTracked: product.isStockTracked,
          isLotTracked: product.isLotTracked,
          isSerialTracked: product.isSerialTracked,
          minStock: product.minStock,
          maxStock: product.maxStock,
          reorderPoint: product.reorderPoint,
          safetyStock: product.safetyStock,
          leadTimeDays: product.leadTimeDays,
          weightKg: product.weightKg,
          widthCm: product.widthCm,
          heightCm: product.heightCm,
          depthCm: product.depthCm,
          volumeM3: product.volumeM3,
          status: values.isActive ? 'Active' : 'Discontinued',
          launchDate: product.launchDate,
          endOfLifeDate: product.endOfLifeDate,
        },
        {
          onSuccess: (response) => {
            if (response.isSuccess) {
              toast.success(t('products.toast.updated'));
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
          toast.success(t('products.toast.created'));
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
        className="w-full max-w-lg overflow-hidden rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-3 dark:border-slate-800">
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {isEdit ? t('products.modal.editTitle') : t('products.modal.createTitle')}
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

        <form onSubmit={onSubmit} noValidate className="space-y-3 px-5 py-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Input
              label={t('products.fields.sku')}
              placeholder={t('products.fields.skuPlaceholder')}
              error={translateError(errors.sku?.message)}
              {...register('sku')}
            />
            <Input
              label={t('products.fields.unit')}
              placeholder={t('products.fields.unitPlaceholder')}
              error={translateError(errors.unit?.message)}
              {...register('unit')}
            />
          </div>

          <Input
            label={t('products.fields.name')}
            placeholder={t('products.fields.namePlaceholder')}
            error={translateError(errors.name?.message)}
            {...register('name')}
          />

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('products.fields.description')}
            </label>
            <textarea
              rows={2}
              placeholder={t('products.fields.descriptionPlaceholder')}
              className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder-slate-500"
              {...register('description')}
            />
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Input
              label={t('products.fields.price')}
              type="number"
              step="0.0001"
              error={translateError(errors.price?.message)}
              {...register('price', { valueAsNumber: true })}
            />
            <Input
              label={t('products.fields.currency')}
              placeholder="USD"
              error={translateError(errors.currency?.message)}
              {...register('currency')}
            />
            <Input
              label={t('products.fields.stockQuantity')}
              type="number"
              step="0.0001"
              error={translateError(errors.stockQuantity?.message)}
              {...register('stockQuantity', { valueAsNumber: true })}
            />
          </div>

          {isEdit && (
            <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-indigo-600"
                {...register('isActive')}
              />
              {t('products.fields.isActive')}
            </label>
          )}

          <div className="flex justify-end gap-2 pt-2">
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
