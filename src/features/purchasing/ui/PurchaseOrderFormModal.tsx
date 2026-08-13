import { useEffect, useMemo, useRef, useState } from 'react';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { LocalizedDateInput } from '@/shared/ui/form/LocalizedDateInput';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useDraftAutosave } from '@/shared/hooks/useDraftAutosave';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { computeDocumentTotals } from '@/shared/lib/documentTotals';
import { formatCurrency } from '@/shared/lib/format';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useResolveFxRateQuery } from '@/shared/fx/hooks/useFxRates';
import { DocumentFormLayout } from '@/shared/ui/document-form/DocumentFormLayout';
import { DocumentLineTable } from '@/shared/ui/document-form/DocumentLineTable';
import { FormWizardSteps } from '@/shared/ui/document-form/FormWizardSteps';
import {
  documentFieldCls as fieldCls,
  documentLabelCls as labelCls,
  documentSectionBodyCls as sectionBodyCls,
  documentSectionHeaderCls as sectionHeaderCls,
  documentSectionTitleCls as sectionTitleCls,
  documentSectionWrapperCls as sectionWrapperCls,
} from '@/shared/ui/document-form/documentFormClasses';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import { useCreatePurchaseOrder, useUpdatePurchaseOrder } from '../hooks/usePurchaseOrders';
import { purchaseOrderSchema, type PurchaseOrderFormValues } from '../model/purchaseOrderSchema';
import type { PurchaseOrder, PurchaseOrderLineInput } from '../model/purchaseOrder.types';
import { PurchaseOrderLineEditor } from './PurchaseOrderLineEditor';

interface Props {
  order: PurchaseOrder | null;
  onClose: () => void;
}

const LINE_HEADER_GRID_CLS =
  'lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5.5rem,0.9fr)]';

const PO_DRAFT_KEY = 'corealign:draft:purchase-order-create';

const todayIso = () => new Date().toISOString().slice(0, 10);

const numOrUndefined = (value?: string): number | undefined =>
  value && Number(value) ? Number(value) : undefined;

const emptyLine = () => ({
  productId: '',
  quantity: 1,
  unitCost: 0,
  taxRatePercent: '',
  lineNotes: '',
});

const emptyValues = (): PurchaseOrderFormValues => ({
  vendorId: '',
  orderDate: todayIso(),
  expectedDate: '',
  currency: 'TRY',
  exchangeRate: '',
  warehouseId: '',
  notes: '',
  lines: [emptyLine()],
});

const fromOrder = (order: PurchaseOrder): PurchaseOrderFormValues => ({
  vendorId: order.vendorId,
  orderDate: order.orderDate.slice(0, 10),
  expectedDate: order.expectedDate?.slice(0, 10) ?? '',
  currency: order.currency,
  exchangeRate: order.exchangeRate ? String(order.exchangeRate) : '',
  warehouseId: order.warehouseId ?? '',
  notes: order.notes ?? '',
  lines:
    order.lines.length > 0
      ? order.lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitCost: l.unitCost,
          taxRatePercent: l.taxRatePercent ? String(l.taxRatePercent) : '',
          lineNotes: l.lineNotes ?? '',
        }))
      : [emptyLine()],
});

export const PurchaseOrderFormModal = ({ order, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const isEdit = order !== null;

  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const warehousesQuery = useWarehousesQuery(true);
  const createMutation = useCreatePurchaseOrder();
  const updateMutation = useUpdatePurchaseOrder();

  const products = productsQuery.data?.data?.items ?? [];
  const vendors = vendorsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    trigger,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<PurchaseOrderFormValues>({
    resolver: zodResolver(purchaseOrderSchema),
    defaultValues: order ? fromOrder(order) : emptyValues(),
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, true);
  const backdrop = useBackdropClick(requestClose);

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  const allValues = useWatch({ control }) as PurchaseOrderFormValues;
  const draft = useDraftAutosave<PurchaseOrderFormValues>(PO_DRAFT_KEY, allValues, {
    enabled: !isEdit && isDirty,
  });

  const [step, setStep] = useState<1 | 2>(1);
  const [draftToRestore, setDraftToRestore] = useState<PurchaseOrderFormValues | null>(null);
  const [mounted, setMounted] = useState(false);
  if (!mounted) {
    setMounted(true);
    setDraftToRestore(isEdit ? null : draft.peekDraft());
  }

  const appliedFxRef = useRef<string | null>(null);
  useEffect(() => {
    reset(order ? fromOrder(order) : emptyValues());
    // WHY the guard is cleared here: a reset blanks the rate field, and under StrictMode's
    // double mount the reset runs a second time while the apply-once ref still remembers the
    // first pass — the field would stay empty and a foreign PO would book at rate 1.
    appliedFxRef.current = null;
  }, [order, reset]);

  const handleStepClick = async (targetStep: 1 | 2) => {
    if (targetStep === 2 && step === 1) {
      const isValid = await trigger(['vendorId', 'orderDate', 'currency']);
      if (!isValid) return;
    }
    setStep(targetStep);
  };

  const watchedLines = useWatch({ control, name: 'lines' });
  const watchedCurrency = useWatch({ control, name: 'currency' });
  const watchedOrderDate = useWatch({ control, name: 'orderDate' });
  const currency = (watchedCurrency || 'TRY').toUpperCase();

  // WHY the rate is resolved here and not left at 1: the goods-receipt and vendor-bill GL legs
  // convert the document amount with po.ExchangeRate, so a foreign PO saved without a rate books
  // its foreign amount as if it were base currency.
  const fxRateQuery = useResolveFxRateQuery(
    !isEdit && currency && currency !== 'TRY' ? currency : undefined,
    watchedOrderDate || undefined,
  );
  const fxSnapshot =
    !isEdit && fxRateQuery.data?.currencyCode === currency ? fxRateQuery.data : null;
  useEffect(() => {
    if (isEdit) return;
    if (currency === 'TRY') {
      if (appliedFxRef.current !== 'TRY') {
        appliedFxRef.current = 'TRY';
        setValue('exchangeRate', '1');
      }
      return;
    }
    if (!fxSnapshot) return;
    const key = `${fxSnapshot.currencyCode}:${fxSnapshot.effectiveDate}`;
    if (appliedFxRef.current === key) return;
    appliedFxRef.current = key;
    setValue('exchangeRate', String(fxSnapshot.sellingRate), { shouldDirty: true });
  }, [isEdit, currency, fxSnapshot, setValue]);

  const summary = useMemo(
    () =>
      computeDocumentTotals({
        lines: (watchedLines ?? []).map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitCost,
          taxRatePercent: l.taxRatePercent,
        })),
      }),
    [watchedLines],
  );

  const handleVendorSelect = (vendorId: string) => {
    setValue('vendorId', vendorId, { shouldValidate: true, shouldDirty: true });
    if (isEdit) return;
    const vendor = vendors.find((v) => v.id === vendorId);
    if (vendor?.defaultCurrency) {
      setValue('currency', vendor.defaultCurrency.toUpperCase(), { shouldDirty: true });
    }
  };

  const handleProductSelect = (index: number, productId: string) => {
    setValue(`lines.${index}.productId`, productId, { shouldValidate: true, shouldDirty: true });
    const product = products.find((p) => p.id === productId);
    if (product && !isEdit) {
      setValue(`lines.${index}.unitCost`, product.lastPurchaseCost || product.standardCost || 0, {
        shouldDirty: true,
      });
    }
  };

  const onSubmit = handleSubmit(
    async (values) => {
      const lines: PurchaseOrderLineInput[] = values.lines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitCost: l.unitCost,
        taxRatePercent: numOrUndefined(l.taxRatePercent) ?? 0,
        lineNotes: l.lineNotes || null,
      }));

      const payload = {
        vendorId: values.vendorId,
        orderDate: new Date(values.orderDate).toISOString(),
        currency: values.currency.toUpperCase(),
        expectedDate: values.expectedDate ? new Date(values.expectedDate).toISOString() : null,
        exchangeRate: numOrUndefined(values.exchangeRate) ?? 1,
        warehouseId: values.warehouseId || null,
        notes: values.notes?.trim() || null,
        lines,
      };

      try {
        if (isEdit && order) {
          await updateMutation.mutateAsync({ id: order.id, ...payload });
          toast.success(t('po.form.updated'));
        } else {
          await createMutation.mutateAsync(payload);
          draft.clearDraft();
          toast.success(t('po.form.created'));
        }
        onClose();
      } catch (err) {
        toastApiError(err);
      }
    },
    (formErrors) => {
      setStep(Object.keys(formErrors).some((k) => k !== 'lines') ? 1 : 2);
    },
  );

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const isBusy = isSubmitting || createMutation.isPending || updateMutation.isPending;
  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  const stepNavigation = (
    <FormWizardSteps
      steps={[
        { id: 1, label: t('po.form.tabs.info') },
        { id: 2, label: t('po.form.tabs.lines') },
      ]}
      current={step}
      onSelect={(id) => void handleStepClick(id as 1 | 2)}
      ariaLabel={t('po.form.newTitle')}
    />
  );

  const footer = (
    <>
      <div className="text-sm">
        <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('po.form.total')}
        </span>{' '}
        <span className="font-semibold text-slate-900 dark:text-slate-100">
          {formatCurrency(summary.grandTotal, locale, currency)}
        </span>
        {!isEdit && draft.lastSavedAt && (
          <div className="text-[10px] text-slate-400 dark:text-slate-500">
            {t('po.form.draft.savedAt', {
              time: new Date(draft.lastSavedAt).toLocaleTimeString(locale),
            })}
          </div>
        )}
      </div>
      <div className="flex items-center gap-3">
        {step === 1 ? (
          <button
            type="button"
            onClick={requestClose}
            className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-500 transition-colors hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
          >
            {t('common.cancel')}
          </button>
        ) : (
          <button
            type="button"
            onClick={() => setStep(1)}
            className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-600 transition-colors hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
          >
            {t('common.back')}
          </button>
        )}
        {step < 2 ? (
          <Button type="button" onClick={() => void handleStepClick(2)}>
            {t('common.next')}
          </Button>
        ) : (
          <Button type="submit" isLoading={isBusy}>
            {t('common.save')}
          </Button>
        )}
      </div>
    </>
  );

  return (
    <DocumentFormLayout
      presentation="modal"
      title={isEdit ? `${t('po.form.editTitle')} ${order.poNumber}` : t('po.form.newTitle')}
      closeAriaLabel={t('common.cancel')}
      onRequestClose={requestClose}
      backdropProps={backdrop}
      stepNavigation={stepNavigation}
      onSubmit={onSubmit}
      onKeyDown={onFormKeyDown}
      footer={footer}
    >
      {draftToRestore && (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-primary-200 bg-primary-50 px-3 py-2 text-xs dark:border-primary-500/30 dark:bg-primary-500/10">
          <span className="text-primary-800 dark:text-primary-200">{t('po.form.draft.found')}</span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => {
                reset(draftToRestore);
                setDraftToRestore(null);
              }}
              className="rounded bg-primary-600 px-2 py-1 font-medium text-white hover:bg-primary-700"
            >
              {t('po.form.draft.restore')}
            </button>
            <button
              type="button"
              onClick={() => {
                draft.clearDraft();
                setDraftToRestore(null);
              }}
              className="rounded px-2 py-1 font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              {t('po.form.draft.discard')}
            </button>
          </div>
        </div>
      )}

      <div
        className={
          step === 1
            ? 'grid w-full grid-cols-[repeat(auto-fit,minmax(min(100%,22rem),1fr))] items-stretch gap-4 pb-2'
            : 'hidden'
        }
      >
        <div className="contents">
          <div className={sectionWrapperCls}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('po.form.sections.general')}</h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 gap-5 sm:grid-cols-2`}>
              <div className="col-span-1 sm:col-span-2">
                <label className={labelCls}>{t('po.form.vendor')}</label>
                <select
                  className={fieldCls}
                  value={allValues.vendorId ?? ''}
                  onChange={(e) => handleVendorSelect(e.target.value)}
                >
                  <option value="">{t('po.form.selectVendor')}</option>
                  {vendors.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.name}
                    </option>
                  ))}
                </select>
                {errors.vendorId?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.vendorId.message)}
                  </span>
                )}
              </div>

              <div>
                <label className={labelCls}>{t('po.form.orderDate')}</label>
                <Controller
                  name="orderDate"
                  control={control}
                  render={({ field }) => (
                    <LocalizedDateInput
                      ref={field.ref}
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      locale={locale}
                      ariaLabel={t('po.form.orderDate')}
                    />
                  )}
                />
                {errors.orderDate?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.orderDate.message)}
                  </span>
                )}
              </div>

              <div>
                <label className={labelCls}>{t('po.form.expectedDate')}</label>
                <Controller
                  name="expectedDate"
                  control={control}
                  render={({ field }) => (
                    <LocalizedDateInput
                      ref={field.ref}
                      value={field.value ?? ''}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      locale={locale}
                      ariaLabel={t('po.form.expectedDate')}
                    />
                  )}
                />
              </div>

              <div>
                <label className={labelCls}>{t('po.form.warehouse')}</label>
                <select className={fieldCls} {...register('warehouseId')}>
                  <option value="">{t('po.form.selectWarehouse')}</option>
                  {warehouses.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name} ({w.code})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className={labelCls}>{t('po.form.currency')}</label>
                <Controller
                  name="currency"
                  control={control}
                  render={({ field }) => (
                    <CurrencySelect value={field.value} onChange={field.onChange} />
                  )}
                />
                {errors.currency?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.currency.message)}
                  </span>
                )}
              </div>

              <div className="col-span-1 sm:col-span-2">
                <label className={labelCls}>{t('po.form.exchangeRate')}</label>
                <input
                  className={fieldCls}
                  type="number"
                  step="0.0001"
                  min="0"
                  {...register('exchangeRate')}
                />
                {fxSnapshot && (
                  <p className="mt-1 text-[10px] text-slate-500">
                    {t('po.form.fxAutoRate', {
                      source: fxSnapshot.source,
                      date: new Date(fxSnapshot.effectiveDate).toLocaleDateString(locale),
                    })}
                  </p>
                )}
              </div>
            </div>
          </div>

          <div className={sectionWrapperCls}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('po.form.sections.notes')}</h3>
            </div>
            <div className={sectionBodyCls}>
              <label className={labelCls}>{t('po.form.notes')}</label>
              <textarea rows={4} className={fieldCls} maxLength={2000} {...register('notes')} />
            </div>
          </div>
        </div>
      </div>

      <div
        className={
          step === 2
            ? 'grid min-h-full grid-cols-1 items-stretch gap-4 pb-2 xl:grid-cols-[minmax(0,7fr)_minmax(17rem,2fr)]'
            : 'hidden'
        }
      >
        <div className="flex min-w-0 flex-col gap-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-200">
              {t('po.form.tabs.lines')}
            </h2>
            <button
              type="button"
              onClick={() => append(emptyLine())}
              className="flex items-center gap-1.5 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm shadow-indigo-500/20 transition-colors hover:bg-indigo-500"
            >
              <Plus size={16} />
              {t('po.form.addLine')}
            </button>
          </div>

          <DocumentLineTable
            headerGridCls={LINE_HEADER_GRID_CLS}
            error={errors.lines?.message ? translateError(errors.lines.message) : undefined}
            header={
              <>
                <div>{t('po.form.product')}</div>
                <div className="grid min-w-0 grid-cols-[minmax(0,0.7fr)_minmax(0,1.2fr)_minmax(0,0.75fr)] gap-2">
                  <div className="text-right">{t('po.form.qty')}</div>
                  <div className="text-right">{t('po.form.unitCost')}</div>
                  <div className="text-right">{t('po.form.tax')}</div>
                </div>
                <div aria-hidden="true" />
                <div className="text-right">{t('po.form.total')}</div>
              </>
            }
          >
            {fields.map((field, index) => (
              <PurchaseOrderLineEditor
                key={field.id}
                index={index}
                register={register}
                errors={errors.lines?.[index]}
                line={watchedLines?.[index]}
                products={products}
                canRemove={fields.length > 1}
                locale={locale}
                currency={currency}
                onProductSelect={handleProductSelect}
                onRemove={remove}
              />
            ))}
          </DocumentLineTable>
        </div>

        <div className="h-full min-w-0">
          <div className={`${sectionWrapperCls} h-full overflow-hidden`}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('po.form.sections.summary')}</h3>
            </div>
            <div className="space-y-4 p-5">
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">{t('po.form.subtotal')}</span>
                <span className="font-medium text-slate-900 dark:text-slate-200">
                  {formatCurrency(summary.subtotal, locale, currency)}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">
                  {summary.taxPct !== null
                    ? t('po.form.taxTotalWithRate', { pct: summary.taxPct })
                    : t('po.form.taxTotal')}
                </span>
                <span className="font-medium text-slate-900 dark:text-slate-200">
                  {formatCurrency(summary.tax, locale, currency)}
                </span>
              </div>
              <div className="mt-2 border-t border-slate-200 pt-5 dark:border-[#2a3143]">
                <div className="flex items-end justify-between">
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-300">
                    {t('po.form.total')}
                  </span>
                  <span className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
                    {formatCurrency(summary.grandTotal, locale, currency)}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </DocumentFormLayout>
  );
};
