import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { ModalTabs } from '@/shared/ui/ModalTabs/ModalTabs';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { getErroredTabs, firstErroredTab } from '@/shared/lib/formTabs';
import { toastApiError } from '@/shared/lib/mutationToast';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import {
  MasterDataQuickModal,
  type QuickAddKind,
} from '@/shared/master-data/ui/MasterDataQuickModal';
import {
  useBrandsQuery,
  useCategoriesQuery,
  useTaxRatesQuery,
  useUomsQuery,
} from '@/shared/master-data/hooks/useMasterData';
import { UnitOfMeasureSelect } from '@/shared/ui/form/UnitOfMeasureSelect';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import { productSchema, type ProductFormValues } from '../model/productSchema';
import type {
  ProcurementType,
  CostingMethod,
  ProductStatus,
  Product,
} from '../model/product.types';
import { useCreateProduct, useUpdateProduct } from '../hooks/useProductQueries';

type ProductIdField =
  | 'brandId'
  | 'categoryId'
  | 'baseUomId'
  | 'salesUomId'
  | 'purchaseUomId'
  | 'taxRateId';
type ProductQuickAdd = { kind: QuickAddKind; field: ProductIdField };

type ProductTab = 'general' | 'pricing' | 'logistics';

const PRODUCT_FIELD_TAB: Record<string, ProductTab> = {
  sku: 'general',
  name: 'general',
  barcode: 'general',
  mpn: 'general',
  brandId: 'general',
  categoryId: 'general',
  status: 'general',
  shortDescription: 'general',
  description: 'general',
  unit: 'general',
  baseUomId: 'general',
  salesUomId: 'general',
  purchaseUomId: 'general',
  price: 'pricing',
  listPrice: 'pricing',
  minSellingPrice: 'pricing',
  standardCost: 'pricing',
  currency: 'pricing',
  taxRateId: 'pricing',
  isPriceTaxInclusive: 'pricing',
  stockQuantity: 'pricing',
  isStockTracked: 'pricing',
  isLotTracked: 'pricing',
  isSerialTracked: 'pricing',
  minStock: 'pricing',
  maxStock: 'pricing',
  reorderPoint: 'pricing',
  safetyStock: 'pricing',
  leadTimeDays: 'pricing',
  procurementType: 'pricing',
  costingMethod: 'pricing',
  color: 'logistics',
  thicknessMm: 'logistics',
  weightKg: 'logistics',
  widthCm: 'logistics',
  heightCm: 'logistics',
  depthCm: 'logistics',
  volumeM3: 'logistics',
  launchDate: 'logistics',
  endOfLifeDate: 'logistics',
  isActive: 'logistics',
};

interface Props {
  open: boolean;
  product: Product | null;
  onClose: () => void;
}

const PRODUCT_STATUSES: ProductStatus[] = ['Active', 'New', 'Discontinued', 'EndOfLife'];
const PROCUREMENT_TYPES: ProcurementType[] = ['Buy', 'Make'];
// Standard cost is intentionally not offered yet: it requires an issue-time cost-variance GL leg
// (and a seeded variance account) to keep inventory netting at actual — shipped separately so the
// selector never exposes a method that would silently behave as weighted-average.
const COSTING_METHODS: CostingMethod[] = ['WeightedAverage', 'Fifo'];

const fieldCls =
  'w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';
const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';
const quickAddBtnCls =
  'inline-flex items-center gap-0.5 rounded px-1.5 py-0.5 text-[10px] font-medium text-primary-600 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-500/10';
const sectionCls = 'space-y-3 border-t border-slate-200 pt-3 dark:border-slate-800';
const sectionTitleCls = 'text-xs font-semibold uppercase tracking-wider text-slate-500';
const checkboxLabelCls =
  'flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200 select-none';

const emptyValues: ProductFormValues = {
  sku: '',
  name: '',
  description: '',
  shortDescription: '',
  barcode: '',
  mpn: '',
  brandId: '',
  categoryId: '',
  status: 'Active',
  unit: 'ADET',
  baseUomId: '',
  salesUomId: '',
  purchaseUomId: '',
  price: 0,
  listPrice: '',
  minSellingPrice: '',
  standardCost: '',
  currency: 'USD',
  taxRateId: '',
  isPriceTaxInclusive: false,
  stockQuantity: 0,
  isStockTracked: true,
  isLotTracked: false,
  isSerialTracked: false,
  minStock: '',
  maxStock: '',
  reorderPoint: '',
  safetyStock: '',
  leadTimeDays: '',
  procurementType: 'Buy',
  costingMethod: 'WeightedAverage',
  color: '',
  thicknessMm: '',
  weightKg: '',
  widthCm: '',
  heightCm: '',
  depthCm: '',
  volumeM3: '',
  launchDate: '',
  endOfLifeDate: '',
  isActive: true,
};

const str = (value?: number | null): string =>
  value !== null && value !== undefined && value !== 0 ? String(value) : '';
const numOrUndef = (value?: string): number | undefined =>
  value && !Number.isNaN(Number(value)) ? Number(value) : undefined;
const numOrZero = (value?: string): number => numOrUndef(value) ?? 0;
const numOrNull = (value?: string): number | null => numOrUndef(value) ?? null;

export const ProductFormModal = ({ open, product, onClose }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateProduct();
  const updateMutation = useUpdateProduct();
  const isEdit = product !== null;

  const brands = useBrandsQuery(true).data?.data ?? [];
  const categories = useCategoriesQuery(true).data?.data ?? [];
  const uoms = useUomsQuery(true).data?.data ?? [];
  const taxRates = useTaxRatesQuery(true).data?.data ?? [];
  const decimals = useDecimalPlaces();
  const step = (1 / Math.pow(10, decimals)).toString();

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: emptyValues,
    mode: 'onTouched',
  });

  const [quickAdd, setQuickAdd] = useState<ProductQuickAdd | null>(null);
  const [tab, setTab] = useState<ProductTab>('general');
  const requestClose = useModalClose(isDirty, onClose, open);
  const backdrop = useBackdropClick(requestClose);
  const erroredTabs = getErroredTabs(errors, PRODUCT_FIELD_TAB);

  useEffect(() => {
    if (!open) return;
    if (product) {
      reset({
        sku: product.sku,
        name: product.name,
        description: product.description ?? '',
        shortDescription: product.shortDescription ?? '',
        barcode: product.barcode ?? '',
        mpn: product.mpn ?? '',
        brandId: product.brandId ?? '',
        categoryId: product.categoryId ?? '',
        status: product.status,
        unit: product.unit,
        baseUomId: product.baseUomId ?? '',
        salesUomId: product.salesUomId ?? '',
        purchaseUomId: product.purchaseUomId ?? '',
        price: product.price,
        listPrice: str(product.listPrice),
        minSellingPrice: str(product.minSellingPrice),
        standardCost: str(product.standardCost),
        currency: product.currency,
        taxRateId: product.taxRateId ?? '',
        isPriceTaxInclusive: product.isPriceTaxInclusive,
        stockQuantity: product.stockQuantity,
        isStockTracked: product.isStockTracked,
        isLotTracked: product.isLotTracked,
        isSerialTracked: product.isSerialTracked,
        minStock: str(product.minStock),
        maxStock: str(product.maxStock),
        reorderPoint: str(product.reorderPoint),
        safetyStock: str(product.safetyStock),
        leadTimeDays: str(product.leadTimeDays),
        procurementType: product.procurementType ?? 'Buy',
        costingMethod: product.costingMethod ?? 'WeightedAverage',
        color: product.color ?? '',
        thicknessMm: str(product.thicknessMm),
        weightKg: str(product.weightKg),
        widthCm: str(product.widthCm),
        heightCm: str(product.heightCm),
        depthCm: str(product.depthCm),
        volumeM3: str(product.volumeM3),
        launchDate: product.launchDate?.slice(0, 10) ?? '',
        endOfLifeDate: product.endOfLifeDate?.slice(0, 10) ?? '',
        isActive: product.isActive,
      });
    } else {
      reset(emptyValues);
    }
  }, [open, product, reset]);

  const onSubmit = handleSubmit(
    (values) => {
      const base = {
        sku: values.sku,
        name: values.name,
        description: values.description || null,
        shortDescription: values.shortDescription || null,
        barcode: values.barcode || null,
        mpn: values.mpn || null,
        brandId: values.brandId || null,
        categoryId: values.categoryId || null,
        unit: values.unit,
        baseUomId: values.baseUomId || null,
        purchaseUomId: values.purchaseUomId || null,
        salesUomId: values.salesUomId || null,
        price: values.price,
        currency: values.currency.toUpperCase(),
        taxRateId: values.taxRateId || null,
        status: values.status,
        procurementType: values.procurementType,
        costingMethod: values.costingMethod,
        color: values.color || null,
        thicknessMm: numOrNull(values.thicknessMm),
        launchDate: values.launchDate || null,
        endOfLifeDate: values.endOfLifeDate || null,
      };

      if (isEdit && product) {
        updateMutation.mutate(
          {
            ...base,
            id: product.id,
            expectedConcurrencyToken: product.concurrencyToken,
            slug: product.slug,
            parentProductId: product.parentProductId,
            variantAttributesJson: product.variantAttributesJson,
            tagsJson: product.tagsJson,
            listPrice: numOrZero(values.listPrice) || values.price,
            minSellingPrice: numOrZero(values.minSellingPrice),
            standardCost: numOrZero(values.standardCost),
            isPriceTaxInclusive: values.isPriceTaxInclusive,
            isStockTracked: values.isStockTracked,
            isLotTracked: values.isLotTracked,
            isSerialTracked: values.isSerialTracked,
            minStock: numOrZero(values.minStock),
            maxStock: numOrZero(values.maxStock),
            reorderPoint: numOrZero(values.reorderPoint),
            safetyStock: numOrZero(values.safetyStock),
            leadTimeDays: numOrZero(values.leadTimeDays),
            weightKg: numOrNull(values.weightKg),
            widthCm: numOrNull(values.widthCm),
            heightCm: numOrNull(values.heightCm),
            depthCm: numOrNull(values.depthCm),
            volumeM3: numOrNull(values.volumeM3),
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

      createMutation.mutate(
        {
          ...base,
          stockQuantity: values.stockQuantity,
          listPrice: numOrUndef(values.listPrice),
          minSellingPrice: numOrUndef(values.minSellingPrice),
          standardCost: numOrUndef(values.standardCost),
          isPriceTaxInclusive: values.isPriceTaxInclusive,
          isStockTracked: values.isStockTracked,
          isLotTracked: values.isLotTracked,
          isSerialTracked: values.isSerialTracked,
          minStock: numOrUndef(values.minStock),
          maxStock: numOrUndef(values.maxStock),
          reorderPoint: numOrUndef(values.reorderPoint),
          safetyStock: numOrUndef(values.safetyStock),
          leadTimeDays: numOrUndef(values.leadTimeDays),
          weightKg: numOrNull(values.weightKg),
          widthCm: numOrNull(values.widthCm),
          heightCm: numOrNull(values.heightCm),
          depthCm: numOrNull(values.depthCm),
          volumeM3: numOrNull(values.volumeM3),
        },
        {
          onSuccess: (response) => {
            if (response.isSuccess) {
              toast.success(t('products.toast.created'));
              onClose();
              return;
            }
            toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
          },
          onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
        },
      );
    },
    (formErrors) => {
      const target = firstErroredTab(formErrors, PRODUCT_FIELD_TAB, [
        'general',
        'pricing',
        'logistics',
      ]);
      if (target) setTab(target as ProductTab);
    },
  );

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (!open) return null;

  const isBusy = isSubmitting || createMutation.isPending || updateMutation.isPending;
  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      {...backdrop}
      role="presentation"
    >
      <div
        className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-200 bg-white px-5 py-3 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {isEdit ? t('products.modal.editTitle') : t('products.modal.createTitle')}
          </h2>
          <button
            type="button"
            onClick={requestClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.cancel')}
          >
            <X size={18} />
          </button>
        </div>

        <ModalTabs
          tabs={[
            {
              id: 'general',
              label: t('products.tabs.general', { defaultValue: 'Genel' }),
              hasError: erroredTabs.has('general'),
            },
            {
              id: 'pricing',
              label: t('products.tabs.pricing', { defaultValue: 'Fiyat & Stok' }),
              hasError: erroredTabs.has('pricing'),
            },
            {
              id: 'logistics',
              label: t('products.tabs.logistics', { defaultValue: 'Lojistik & Yaşam Döngüsü' }),
              hasError: erroredTabs.has('logistics'),
            },
          ]}
          active={tab}
          onChange={(id) => setTab(id as typeof tab)}
        />

        <form
          onSubmit={onSubmit}
          onKeyDown={onFormKeyDown}
          noValidate
          className="space-y-4 px-5 py-4"
        >
          <div className={tab === 'general' ? 'space-y-4' : 'hidden'}>
            <section className="space-y-3">
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Input
                  label={`${t('products.fields.sku')} *`}
                  placeholder={t('products.fields.skuPlaceholder')}
                  autoFocus
                  error={translateError(errors.sku?.message)}
                  {...register('sku')}
                />
                <Input label={t('products.fields.barcode')} {...register('barcode')} />
              </div>
              <Input
                label={`${t('products.fields.name')} *`}
                placeholder={t('products.fields.namePlaceholder')}
                error={translateError(errors.name?.message)}
                {...register('name')}
              />
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Input label={t('products.fields.mpn')} {...register('mpn')} />
                <div>
                  <label className={labelCls}>{t('products.fields.status')}</label>
                  <select className={fieldCls} {...register('status')}>
                    {PRODUCT_STATUSES.map((s) => (
                      <option key={s} value={s}>
                        {t(`products.status.${s}` as never)}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <LabelWithAdd
                    label={t('products.fields.brand')}
                    onAdd={() => setQuickAdd({ kind: 'brand', field: 'brandId' })}
                  />
                  <select className={fieldCls} {...register('brandId')}>
                    <option value="">{t('products.fields.none')}</option>
                    {brands.map((b) => (
                      <option key={b.id} value={b.id}>
                        {b.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <LabelWithAdd
                    label={t('products.fields.category')}
                    onAdd={() => setQuickAdd({ kind: 'category', field: 'categoryId' })}
                  />
                  <select className={fieldCls} {...register('categoryId')}>
                    <option value="">{t('products.fields.none')}</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div>
                <label className={labelCls}>{t('products.fields.shortDescription')}</label>
                <textarea rows={2} className={fieldCls} {...register('shortDescription')} />
              </div>
              <div>
                <label className={labelCls}>{t('products.fields.description')}</label>
                <textarea
                  rows={2}
                  placeholder={t('products.fields.descriptionPlaceholder')}
                  className={fieldCls}
                  {...register('description')}
                />
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('products.sections.uom')}</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
                <div>
                  <label className={labelCls}>{t('products.fields.unit')}</label>
                  <Controller
                    control={control}
                    name="unit"
                    render={({ field }) => (
                      <UnitOfMeasureSelect
                        value={field.value ?? ''}
                        onChange={field.onChange}
                        className={fieldCls}
                        placeholder={t('products.fields.unitPlaceholder')}
                      />
                    )}
                  />
                  {errors.unit?.message && (
                    <p className="mt-1 text-xs text-danger-600 dark:text-danger-400">
                      {translateError(errors.unit.message)}
                    </p>
                  )}
                </div>
                <div>
                  <LabelWithAdd
                    label={t('products.fields.baseUom')}
                    onAdd={() => setQuickAdd({ kind: 'uom', field: 'baseUomId' })}
                  />
                  <select className={fieldCls} {...register('baseUomId')}>
                    <option value="">{t('products.fields.none')}</option>
                    {uoms.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <LabelWithAdd
                    label={t('products.fields.salesUom')}
                    onAdd={() => setQuickAdd({ kind: 'uom', field: 'salesUomId' })}
                  />
                  <select className={fieldCls} {...register('salesUomId')}>
                    <option value="">{t('products.fields.none')}</option>
                    {uoms.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <LabelWithAdd
                    label={t('products.fields.purchaseUom')}
                    onAdd={() => setQuickAdd({ kind: 'uom', field: 'purchaseUomId' })}
                  />
                  <select className={fieldCls} {...register('purchaseUomId')}>
                    <option value="">{t('products.fields.none')}</option>
                    {uoms.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </section>
          </div>

          <div className={tab === 'pricing' ? 'space-y-4' : 'hidden'}>
            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('products.sections.pricing')}</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
                <Input
                  label={t('products.fields.price')}
                  type="number"
                  step={step}
                  error={translateError(errors.price?.message)}
                  {...register('price', { valueAsNumber: true })}
                />
                <Input
                  label={t('products.fields.listPrice')}
                  type="number"
                  step={step}
                  {...register('listPrice')}
                />
                <Input
                  label={t('products.fields.minSellingPrice')}
                  type="number"
                  step={step}
                  {...register('minSellingPrice')}
                />
                <Input
                  label={t('products.fields.standardCost')}
                  type="number"
                  step={step}
                  {...register('standardCost')}
                />
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className={labelCls}>{t('products.fields.currency')}</label>
                  <Controller
                    name="currency"
                    control={control}
                    render={({ field }) => (
                      <CurrencySelect value={field.value} onChange={field.onChange} />
                    )}
                  />
                </div>
                <div>
                  <LabelWithAdd
                    label={t('products.fields.taxRate')}
                    onAdd={() => setQuickAdd({ kind: 'taxRate', field: 'taxRateId' })}
                  />
                  <select className={fieldCls} {...register('taxRateId')}>
                    <option value="">{t('products.fields.none')}</option>
                    {taxRates.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name} ({r.ratePercent}%)
                      </option>
                    ))}
                  </select>
                </div>
                <label className={`${checkboxLabelCls} sm:mt-6`}>
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 text-primary-600"
                    {...register('isPriceTaxInclusive')}
                  />
                  {t('products.fields.taxInclusive')}
                </label>
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('products.sections.stock')}</h3>
              <div className="flex flex-wrap gap-4">
                <label className={checkboxLabelCls}>
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 text-primary-600"
                    {...register('isStockTracked')}
                  />
                  {t('products.fields.isStockTracked')}
                </label>
                <label className={checkboxLabelCls}>
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 text-primary-600"
                    {...register('isLotTracked')}
                  />
                  {t('products.fields.isLotTracked')}
                </label>
                <label className={checkboxLabelCls}>
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 text-primary-600"
                    {...register('isSerialTracked')}
                  />
                  {t('products.fields.isSerialTracked')}
                </label>
              </div>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                <Input
                  label={t('products.fields.stockQuantity')}
                  type="number"
                  step={step}
                  disabled={isEdit}
                  error={translateError(errors.stockQuantity?.message)}
                  {...register('stockQuantity', { valueAsNumber: true })}
                />
                <Input
                  label={t('products.fields.minStock')}
                  type="number"
                  step={step}
                  {...register('minStock')}
                />
                <Input
                  label={t('products.fields.maxStock')}
                  type="number"
                  step={step}
                  {...register('maxStock')}
                />
                <Input
                  label={t('products.fields.reorderPoint')}
                  type="number"
                  step={step}
                  {...register('reorderPoint')}
                />
                <Input
                  label={t('products.fields.safetyStock')}
                  type="number"
                  step={step}
                  {...register('safetyStock')}
                />
                <Input
                  label={t('products.fields.leadTimeDays')}
                  type="number"
                  step="1"
                  {...register('leadTimeDays')}
                />
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('products.sections.procurement')}</h3>
              <Controller
                name="procurementType"
                control={control}
                render={({ field }) => (
                  <div
                    role="radiogroup"
                    aria-label={t('products.fields.procurementType')}
                    className="grid grid-cols-1 gap-3 sm:grid-cols-2"
                  >
                    {PROCUREMENT_TYPES.map((pt) => {
                      const selected = field.value === pt;
                      return (
                        <button
                          key={pt}
                          type="button"
                          role="radio"
                          aria-checked={selected}
                          onClick={() => field.onChange(pt)}
                          className={`flex flex-col gap-1 rounded-lg border p-3 text-left transition ${
                            selected
                              ? pt === 'Make'
                                ? 'border-violet-500 bg-violet-50 dark:border-violet-500 dark:bg-violet-500/10'
                                : 'border-info-500 bg-info-50 dark:border-info-500 dark:bg-info-500/10'
                              : 'border-slate-200 bg-white hover:border-slate-300 dark:border-slate-700 dark:bg-slate-900'
                          }`}
                        >
                          <span className="text-sm font-semibold text-slate-800 dark:text-slate-100">
                            {t(`products.procurementType.${pt}`)}
                          </span>
                          <span className="text-xs text-slate-500 dark:text-slate-400">
                            {t(`products.procurementTypeHint.${pt}`)}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                )}
              />

              <div className="mt-4">
                <label className={labelCls} htmlFor="costingMethod">
                  {t('products.fields.costingMethod')}
                </label>
                <select id="costingMethod" className={fieldCls} {...register('costingMethod')}>
                  {COSTING_METHODS.map((cm) => (
                    <option key={cm} value={cm}>
                      {t(`products.costingMethod.${cm}`)}
                    </option>
                  ))}
                </select>
                <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                  {t('products.costingMethodHint')}
                </p>
              </div>
            </section>
          </div>

          <div className={tab === 'logistics' ? 'space-y-4' : 'hidden'}>
            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('products.sections.logistics')}</h3>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
                <Input label={t('products.fields.color')} {...register('color')} />
                <Input
                  label={t('products.fields.thicknessMm')}
                  type="number"
                  step="0.1"
                  {...register('thicknessMm')}
                />
                <Input
                  label={t('products.fields.weightKg')}
                  type="number"
                  step="0.001"
                  {...register('weightKg')}
                />
                <Input
                  label={t('products.fields.widthCm')}
                  type="number"
                  step="0.1"
                  {...register('widthCm')}
                />
                <Input
                  label={t('products.fields.heightCm')}
                  type="number"
                  step="0.1"
                  {...register('heightCm')}
                />
                <Input
                  label={t('products.fields.depthCm')}
                  type="number"
                  step="0.1"
                  {...register('depthCm')}
                />
                <Input
                  label={t('products.fields.volumeM3')}
                  type="number"
                  step="0.001"
                  {...register('volumeM3')}
                />
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('products.sections.lifecycle')}</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Input
                  label={t('products.fields.launchDate')}
                  type="date"
                  {...register('launchDate')}
                />
                <Input
                  label={t('products.fields.endOfLifeDate')}
                  type="date"
                  {...register('endOfLifeDate')}
                />
              </div>
            </section>
          </div>

          <div className="sticky bottom-0 flex justify-end gap-2 border-t border-slate-200 bg-white pt-3 dark:border-slate-800 dark:bg-slate-900">
            <button
              type="button"
              onClick={requestClose}
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

      {quickAdd && (
        <MasterDataQuickModal
          kind={quickAdd.kind}
          onClose={() => setQuickAdd(null)}
          onCreated={(id) => {
            setValue(quickAdd.field, id, { shouldDirty: true });
            setQuickAdd(null);
          }}
        />
      )}
    </div>
  );
};

const LabelWithAdd = ({ label, onAdd }: { label: string; onAdd: () => void }) => (
  <div className="mb-1 flex items-center justify-between">
    <label className="text-xs font-medium text-slate-700 dark:text-slate-300">{label}</label>
    <button type="button" onClick={onAdd} className={quickAddBtnCls}>
      <Plus size={11} /> Yeni
    </button>
  </div>
);
