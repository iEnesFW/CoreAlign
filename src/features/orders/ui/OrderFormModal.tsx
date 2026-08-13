import { useEffect, useMemo, useRef, useState } from 'react';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { NextNumberBadge } from '@/shared/ui/NextNumberBadge/NextNumberBadge';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useDraftAutosave } from '@/shared/hooks/useDraftAutosave';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { computeDocumentTotals } from '@/shared/lib/documentTotals';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { LocalizedDateInput } from '@/shared/ui/form/LocalizedDateInput';
import { DocumentFormLayout } from '@/shared/ui/document-form/DocumentFormLayout';
import { DocumentLineTable } from '@/shared/ui/document-form/DocumentLineTable';
import { FormWizardSteps } from '@/shared/ui/document-form/FormWizardSteps';
import {
  documentFieldCls as fieldCls,
  documentLabelCls as labelCls,
  documentQuickAddBtnCls as quickAddBtnCls,
  documentSectionBodyCls as sectionBodyCls,
  documentSectionHeaderCls as sectionHeaderCls,
  documentSectionTitleCls as sectionTitleCls,
  documentSectionWrapperCls as sectionWrapperCls,
} from '@/shared/ui/document-form/documentFormClasses';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useResolveFxRateQuery } from '@/shared/fx/hooks/useFxRates';
import { MasterDataQuickModal } from '@/shared/master-data/ui/MasterDataQuickModal';
import {
  useCustomersQuery,
  useCustomerAddressesQuery,
  useCustomerQuery,
} from '@/features/customers/hooks/useCustomerQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import {
  usePaymentTermsQuery,
  usePriceListsQuery,
  usePriceListItemsQuery,
  useTaxRatesQuery,
  useUomsQuery,
  useWarehousesQuery,
  useWithholdingTaxCodesQuery,
} from '@/shared/master-data/hooks/useMasterData';
import type { WithholdingTaxCode } from '@/shared/master-data/model/masterData.types';
import { orderSchema, glassLineArea, type OrderFormValues } from '../model/orderSchema';
import {
  ORDER_STATUSES,
  type Order,
  type OrderLineInput,
  type OrderSource,
  type OrderType,
} from '../model/order.types';
import { useCreateOrder, useUpdateOrder } from '../hooks/useOrderQueries';
import { OrderLineEditor } from './OrderLineEditor';

interface Props {
  open: boolean;
  order: Order | null;
  onClose: () => void;
  presentation?: 'modal' | 'page';
  renderPageHeader?: (stepNavigation: React.ReactNode) => React.ReactNode;
}

const ORDER_TYPES: OrderType[] = ['Standard', 'Blanket', 'Return', 'Sample', 'Internal'];
const ORDER_SOURCES: OrderSource[] = [
  'Manual',
  'Web',
  'Api',
  'Edi',
  'Marketplace',
  'Phone',
  'InStore',
  'Quote',
];

const LINE_HEADER_GRID_CLS =
  'lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5.5rem,0.9fr)]';

const toIsoDate = (date: Date): string =>
  [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0'),
  ].join('-');
const todayIso = () => toIsoDate(new Date());

const ORDER_DRAFT_KEY = 'corealign:draft:order-create';

const emptyValues: OrderFormValues = {
  orderNumber: '',
  customerId: '',
  orderDate: todayIso(),
  status: 'Draft',
  type: 'Standard',
  source: 'Manual',
  currency: 'USD',
  exchangeRate: '',
  requestedDeliveryDate: '',
  promisedDeliveryDate: '',
  paymentTermsId: '',
  priceListId: '',
  billingAddressId: '',
  shippingAddressId: '',
  headerDiscountPercent: '',
  shippingCost: '',
  channel: '',
  internalNotes: '',
  customerNotes: '',
  notes: '',
  lines: [{ productId: '', quantity: 1, unitPrice: 0 }],
};

const toIsoOrNull = (value?: string): string | null =>
  value ? new Date(value).toISOString() : null;
const numOrUndefined = (value?: string): number | undefined =>
  value && Number(value) ? Number(value) : undefined;

export const OrderFormModal = ({
  open,
  order,
  onClose,
  presentation = 'modal',
  renderPageHeader,
}: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const createMutation = useCreateOrder();
  const updateMutation = useUpdateOrder();
  const isEdit = order !== null;
  const isDraft = !isEdit || order?.status === 'Draft';

  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const taxRatesQuery = useTaxRatesQuery(true);
  const warehousesQuery = useWarehousesQuery(true);
  const withholdingCodesQuery = useWithholdingTaxCodesQuery(true);
  const paymentTermsQuery = usePaymentTermsQuery(true);
  const priceListsQuery = usePriceListsQuery(true);
  const uomsQuery = useUomsQuery(true);
  const decimals = useDecimalPlaces();

  const customers = customersQuery.data?.data?.items ?? [];
  const products = productsQuery.data?.data?.items ?? [];
  const taxRates = taxRatesQuery.data?.data ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];
  const withholdingCodes = withholdingCodesQuery.data?.data ?? [];
  const paymentTerms = paymentTermsQuery.data?.data ?? [];
  const priceLists = priceListsQuery.data?.data ?? [];
  const uoms = uomsQuery.data?.data ?? [];

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    getValues,
    trigger,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(orderSchema),
    defaultValues: emptyValues,
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, open);
  const backdrop = useBackdropClick(requestClose);

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  const allValues = useWatch({ control }) as OrderFormValues;

  const [quickAdd, setQuickAdd] = useState<'paymentTerm' | 'priceList' | null>(null);
  const [step, setStep] = useState<1 | 2>(1);
  const handleStepClick = async (targetStep: 1 | 2) => {
    if (targetStep === 2 && step === 1) {
      const isValid = await trigger(['customerId', 'orderDate']);
      if (!isValid) return;
    }
    setStep(targetStep);
  };
  const [manualNumber, setManualNumber] = useState(false);
  const [draftToRestore, setDraftToRestore] = useState<OrderFormValues | null>(null);
  const draft = useDraftAutosave<OrderFormValues>(ORDER_DRAFT_KEY, allValues, {
    enabled: open && !isEdit && isDirty,
  });
  // WHY seeded false and not `open`: the page route mounts with open already true, so seeding
  // from the prop makes this branch unreachable there and the saved draft is never offered back.
  const [seenOpen, setSeenOpen] = useState(false);
  if (open !== seenOpen) {
    setSeenOpen(open);
    setManualNumber(false);
    setDraftToRestore(open && !isEdit ? draft.peekDraft() : null);
  }

  const productRefs = useRef(new Map<string, HTMLInputElement | null>());
  const focusNewLine = useRef(false);
  useEffect(() => {
    if (!focusNewLine.current) return;
    focusNewLine.current = false;
    const lastId = fields[fields.length - 1]?.id;
    if (lastId) productRefs.current.get(lastId)?.focus();
  }, [fields]);

  const addLine = () => {
    focusNewLine.current = true;
    const lastLine = fields.length > 0 ? getValues(`lines.${fields.length - 1}`) : null;
    if (lastLine) {
      append({
        productId: lastLine.productId,
        quantity: 1,
        unitPrice: lastLine.unitPrice,
        taxRateId: lastLine.taxRateId,
        warehouseId: lastLine.warehouseId,
        withholdingTaxCodeId: lastLine.withholdingTaxCodeId,
        lineDiscountPercent: lastLine.lineDiscountPercent,
      });
    } else {
      append({ productId: '', quantity: 1, unitPrice: 0 });
    }
  };

  useEffect(() => {
    if (!open) return;
    if (order) {
      reset({
        orderNumber: order.orderNumber,
        customerId: order.customerId,
        orderDate: order.orderDate.slice(0, 10),
        status: order.status,
        type: order.type,
        source: order.source,
        currency: order.currency,
        exchangeRate: order.exchangeRate ? String(order.exchangeRate) : '',
        requestedDeliveryDate: order.requestedDeliveryDate?.slice(0, 10) ?? '',
        promisedDeliveryDate: order.promisedDeliveryDate?.slice(0, 10) ?? '',
        paymentTermsId: order.paymentTermsId ?? '',
        priceListId: order.priceListId ?? '',
        billingAddressId: order.billingAddressId ?? '',
        shippingAddressId: order.shippingAddressId ?? '',
        headerDiscountPercent: order.headerDiscountPercent
          ? String(order.headerDiscountPercent)
          : '',
        shippingCost: order.shippingCost ? String(order.shippingCost) : '',
        channel: order.channel ?? '',
        internalNotes: order.internalNotes ?? '',
        customerNotes: order.customerNotes ?? '',
        notes: order.notes ?? '',
        lines: order.lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          uomId: l.uomId ?? '',
          uomCode: l.uomCode ?? '',
          lineDiscountPercent: l.lineDiscountPercent ? String(l.lineDiscountPercent) : '',
          taxRateId: l.taxRateId ?? '',
          taxRatePercent: l.taxRatePercent ? String(l.taxRatePercent) : '',
          withholdingRatePercent: l.withholdingRatePercent ? String(l.withholdingRatePercent) : '',
          withholdingTaxCodeId: l.withholdingTaxCodeId ?? '',
          warehouseId: l.warehouseId ?? '',
          lineNotes: l.lineNotes ?? '',
          widthMm: l.widthMm ? String(l.widthMm) : '',
          heightMm: l.heightMm ? String(l.heightMm) : '',
          pieces: l.pieces ? String(l.pieces) : '',
        })),
      });
    } else {
      reset(emptyValues);
    }
  }, [open, order, reset]);

  const watchedLines = useWatch({ control, name: 'lines' });
  const watchedCurrency = useWatch({ control, name: 'currency' });
  const watchedCustomerId = useWatch({ control, name: 'customerId' });
  const watchedHeaderDiscount = useWatch({ control, name: 'headerDiscountPercent' });
  const watchedShipping = useWatch({ control, name: 'shippingCost' });
  const watchedOrderDate = useWatch({ control, name: 'orderDate' });

  const addressesQuery = useCustomerAddressesQuery(watchedCustomerId || null);
  const addresses = addressesQuery.data?.data ?? [];

  const watchedPriceListId = useWatch({ control, name: 'priceListId' });
  const customerQuery = useCustomerQuery(watchedCustomerId || null);
  const priceListItemsQuery = usePriceListItemsQuery(watchedPriceListId || null);
  const priceListPriceByProduct = useMemo(() => {
    const m = new Map<string, number>();
    for (const it of priceListItemsQuery.data?.data ?? []) m.set(it.productId, it.price);
    return m;
  }, [priceListItemsQuery.data]);

  const appliedCustomerRef = useRef<string | null>(null);
  useEffect(() => {
    if (order) return;
    if (!watchedCustomerId) {
      appliedCustomerRef.current = null;
      return;
    }
    const customer = customerQuery.data?.data;
    if (!customer || customer.id !== watchedCustomerId) return;
    if (appliedCustomerRef.current === customer.id) return;
    appliedCustomerRef.current = customer.id;
    if (customer.defaultCurrency) setValue('currency', customer.defaultCurrency.toUpperCase());
    setValue('paymentTermsId', customer.paymentTermsId ?? '');
    setValue('priceListId', customer.priceListId ?? '');
    setValue(
      'headerDiscountPercent',
      customer.defaultDiscountPercent ? String(customer.defaultDiscountPercent) : '',
    );
  }, [customerQuery.data, watchedCustomerId, order, setValue]);

  const fxCurrency = (watchedCurrency || '').toUpperCase();
  const fxRateQuery = useResolveFxRateQuery(
    !isEdit && fxCurrency && fxCurrency !== 'TRY' ? fxCurrency : undefined,
    watchedOrderDate || undefined,
  );
  const fxSnapshot =
    !isEdit && fxRateQuery.data?.currencyCode === fxCurrency ? fxRateQuery.data : null;
  const appliedFxRef = useRef<string | null>(null);
  useEffect(() => {
    if (open) appliedFxRef.current = null;
  }, [open]);
  useEffect(() => {
    if (isEdit) return;
    if (fxCurrency === 'TRY') {
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
  }, [isEdit, fxCurrency, fxSnapshot, setValue]);

  const currency = (watchedCurrency || 'USD').toUpperCase();

  const withholdingCodeById = useMemo(() => {
    const m = new Map<string, WithholdingTaxCode>();
    for (const c of withholdingCodesQuery.data?.data ?? []) m.set(c.id, c);
    return m;
  }, [withholdingCodesQuery.data]);

  const summary = useMemo(
    () =>
      computeDocumentTotals({
        lines: watchedLines,
        headerDiscountPercent: watchedHeaderDiscount,
        shippingCost: watchedShipping,
        withholdingCodeById,
      }),
    [watchedLines, watchedHeaderDiscount, watchedShipping, withholdingCodeById],
  );

  const handleProductSelect = (index: number, productId: string) => {
    setValue(`lines.${index}.productId`, productId, { shouldValidate: true });
    const product = products.find((p) => p.id === productId);
    if (!product) return;
    const listPrice = priceListPriceByProduct.get(productId);
    setValue(`lines.${index}.unitPrice`, listPrice ?? product.price);
    const uom = uoms.find((u) => u.id === product.salesUomId);
    setValue(`lines.${index}.uomId`, product.salesUomId ?? '');
    setValue(`lines.${index}.uomCode`, uom?.code ?? product.unit ?? '');
    const rate = product.taxRateId ? taxRates.find((r) => r.id === product.taxRateId) : undefined;
    setValue(`lines.${index}.taxRateId`, rate?.id ?? '');
    setValue(`lines.${index}.taxRatePercent`, rate ? String(rate.ratePercent) : '');
  };

  const handleTaxRateSelect = (index: number, taxRateId: string) => {
    setValue(`lines.${index}.taxRateId`, taxRateId);
    const rate = taxRates.find((r) => r.id === taxRateId);
    setValue(`lines.${index}.taxRatePercent`, rate ? String(rate.ratePercent) : '');
  };

  const onSubmit = handleSubmit(
    (values) => {
      const unitByProductId = new Map(products.map((p) => [p.id, p.unit]));
      const lines: OrderLineInput[] = values.lines.map((l) => {
        const area = glassLineArea(
          unitByProductId.get(l.productId),
          l.widthMm,
          l.heightMm,
          l.pieces,
        );
        return {
          productId: l.productId,
          quantity: area ?? l.quantity,
          unitPrice: l.unitPrice,
          uomId: l.uomId || null,
          uomCode: l.uomCode || null,
          lineDiscountPercent: numOrUndefined(l.lineDiscountPercent),
          taxRateId: l.taxRateId || null,
          taxRatePercent: numOrUndefined(l.taxRatePercent),
          withholdingRatePercent: numOrUndefined(l.withholdingRatePercent),
          withholdingTaxCodeId: l.withholdingTaxCodeId || null,
          warehouseId: l.warehouseId || null,
          lineNotes: l.lineNotes || null,
          widthMm: numOrUndefined(l.widthMm),
          heightMm: numOrUndefined(l.heightMm),
          pieces: numOrUndefined(l.pieces),
        };
      });

      const payload = {
        orderNumber: values.orderNumber ?? '',
        customerId: values.customerId,
        orderDate: new Date(values.orderDate).toISOString(),
        currency: values.currency.toUpperCase(),
        type: values.type,
        source: values.source,
        exchangeRate: numOrUndefined(values.exchangeRate) ?? 1,
        requestedDeliveryDate: toIsoOrNull(values.requestedDeliveryDate),
        promisedDeliveryDate: toIsoOrNull(values.promisedDeliveryDate),
        paymentTermsId: values.paymentTermsId || null,
        priceListId: values.priceListId || null,
        billingAddressId: values.billingAddressId || null,
        shippingAddressId: values.shippingAddressId || null,
        headerDiscountPercent: numOrUndefined(values.headerDiscountPercent),
        shippingCost: numOrUndefined(values.shippingCost),
        channel: values.channel || null,
        internalNotes: values.internalNotes || null,
        customerNotes: values.customerNotes || null,
        notes: values.notes || null,
        lines,
      };

      if (isEdit && order) {
        updateMutation.mutate(
          {
            ...payload,
            id: order.id,
            status: values.status,
            salesRepUserId: order.salesRepUserId,
            originOrderId: order.originOrderId,
          },
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
            draft.clearDraft();
            onClose();
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      });
    },
    (formErrors) => {
      setStep(Object.keys(formErrors).some((k) => k !== 'lines') ? 1 : 2);
    },
  );

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (!open) return null;

  const isBusy = isSubmitting || createMutation.isPending || updateMutation.isPending;
  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  const quickAddModal = quickAdd ? (
    <MasterDataQuickModal
      kind={quickAdd}
      onClose={() => setQuickAdd(null)}
      onCreated={(id) => {
        setValue(quickAdd === 'paymentTerm' ? 'paymentTermsId' : 'priceListId', id, {
          shouldDirty: true,
        });
        setQuickAdd(null);
      }}
    />
  ) : null;

  const stepNavigation = (
    <FormWizardSteps
      steps={[
        { id: 1, label: t('orders.tabs.info') },
        { id: 2, label: t('orders.tabs.lines') },
      ]}
      current={step}
      onSelect={(id) => void handleStepClick(id as 1 | 2)}
      ariaLabel={t('orders.modal.createTitle')}
    />
  );

  const footer = (
    <>
      <div className="text-sm">
        <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('orders.summary.grandTotal')}
        </span>{' '}
        <span className="font-semibold text-slate-900 dark:text-slate-100">
          {formatCurrency(summary.grandTotal, locale, currency, decimals)}
        </span>
        {!isEdit && draft.lastSavedAt && (
          <div className="text-[10px] text-slate-400 dark:text-slate-500">
            {t('orders.draft.savedAt', {
              defaultValue: 'Taslak kaydedildi {{time}}',
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
            className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800 transition-colors"
          >
            {t('common.cancel')}
          </button>
        ) : (
          <button
            type="button"
            onClick={() => setStep(1)}
            className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800 transition-colors"
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
      presentation={presentation}
      title={isEdit ? t('orders.modal.editTitle') : t('orders.modal.createTitle')}
      closeAriaLabel={t('common.cancel')}
      onRequestClose={requestClose}
      backdropProps={backdrop}
      stepNavigation={stepNavigation}
      renderPageHeader={renderPageHeader}
      onSubmit={onSubmit}
      onKeyDown={onFormKeyDown}
      footer={footer}
      overlay={quickAddModal}
    >
      {!isEdit && draftToRestore && (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-primary-200 bg-primary-50 px-3 py-2 text-xs dark:border-primary-500/30 dark:bg-primary-500/10">
          <span className="text-primary-800 dark:text-primary-200">
            {t('orders.draft.found', { defaultValue: 'Kaydedilmiş bir taslak bulundu.' })}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => {
                reset(draftToRestore);
                setDraftToRestore(null);
              }}
              className="rounded bg-primary-600 px-2 py-1 font-medium text-white hover:bg-primary-700"
            >
              {t('orders.draft.restore', { defaultValue: 'Geri yükle' })}
            </button>
            <button
              type="button"
              onClick={() => {
                draft.clearDraft();
                setDraftToRestore(null);
              }}
              className="rounded px-2 py-1 font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              {t('orders.draft.discard', { defaultValue: 'Yoksay' })}
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
              <h3 className={sectionTitleCls}>
                {t('orders.sections.general', {
                  defaultValue: 'Customer & General Information',
                })}
              </h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 sm:grid-cols-2 gap-5`}>
              <div className="col-span-1 sm:col-span-2">
                <label className={labelCls}>{t('orders.fields.customer')}</label>
                <select disabled={!isDraft} className={fieldCls} {...register('customerId')}>
                  <option value="">{t('orders.fields.customerPlaceholder')}</option>
                  {customers.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
                {errors.customerId?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.customerId.message)}
                  </span>
                )}
              </div>

              {!isEdit && !manualNumber ? (
                <div>
                  <label className={labelCls}>{t('orders.fields.orderNumber')}</label>
                  <div className="flex items-center gap-2">
                    <NextNumberBadge type="OrderNumber" />
                    <button
                      type="button"
                      onClick={() => setManualNumber(true)}
                      className="text-[11px] font-medium text-indigo-400 hover:underline"
                    >
                      {t('numbering.enterManually', { defaultValue: 'Numarayı elle gir' })}
                    </button>
                  </div>
                </div>
              ) : (
                <div>
                  <label className={labelCls}>{t('orders.fields.orderNumber')}</label>
                  <input
                    className={fieldCls}
                    placeholder="ORD-2026-0001"
                    disabled={isEdit && !isDraft}
                    {...register('orderNumber')}
                  />
                  {errors.orderNumber?.message && (
                    <span className="mt-1 block text-[10px] text-danger-500">
                      {translateError(errors.orderNumber.message)}
                    </span>
                  )}
                  {!isEdit && (
                    <button
                      type="button"
                      onClick={() => {
                        setManualNumber(false);
                        setValue('orderNumber', '');
                      }}
                      className="mt-1 text-[11px] font-medium text-slate-500 hover:underline"
                    >
                      {t('numbering.useAutomatic', { defaultValue: 'Otomatik numara kullan' })}
                    </button>
                  )}
                </div>
              )}

              <div>
                <label className={labelCls}>{t('orders.fields.orderDate')}</label>
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
                      ariaLabel={t('orders.fields.orderDate')}
                      disabled={!isDraft}
                    />
                  )}
                />
              </div>

              <div>
                <label className={labelCls}>{t('orders.fields.type')}</label>
                <select disabled={!isDraft} className={fieldCls} {...register('type')}>
                  {ORDER_TYPES.map((ty) => (
                    <option key={ty} value={ty}>
                      {t(`orders.type.${ty}` as never)}
                    </option>
                  ))}
                </select>
                {errors.type?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.type.message)}
                  </span>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className={labelCls}>{t('orders.fields.source')}</label>
                  <select disabled={!isDraft} className={fieldCls} {...register('source')}>
                    {ORDER_SOURCES.map((s) => (
                      <option key={s} value={s}>
                        {t(`orders.source.${s}` as never)}
                      </option>
                    ))}
                  </select>
                  {errors.source?.message && (
                    <span className="mt-1 block text-[10px] text-danger-500">
                      {translateError(errors.source.message)}
                    </span>
                  )}
                </div>
                <div>
                  <label className={labelCls}>{t('orders.fields.status')}</label>
                  <select disabled={!isEdit} className={fieldCls} {...register('status')}>
                    {ORDER_STATUSES.map((s) => (
                      <option key={s} value={s}>
                        {t(`orders.status.${s}` as never)}
                      </option>
                    ))}
                  </select>
                  {errors.status?.message && (
                    <span className="mt-1 block text-[10px] text-danger-500">
                      {translateError(errors.status.message)}
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>

          <div className={sectionWrapperCls}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>
                {t('orders.sections.addresses', { defaultValue: 'Addresses & Delivery' })}
              </h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 sm:grid-cols-2 gap-5`}>
              <div>
                <label className={labelCls}>{t('orders.fields.billingAddress')}</label>
                <select
                  className={fieldCls}
                  disabled={!watchedCustomerId}
                  {...register('billingAddressId')}
                >
                  <option value="">{t('orders.lines.none')}</option>
                  {addresses.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.label} — {a.line1} {a.city ? `, ${a.city}` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelCls}>{t('orders.fields.shippingAddress')}</label>
                <select
                  className={fieldCls}
                  disabled={!watchedCustomerId}
                  {...register('shippingAddressId')}
                >
                  <option value="">{t('orders.lines.none')}</option>
                  {addresses.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.label} — {a.line1} {a.city ? `, ${a.city}` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelCls}>{t('orders.fields.requestedDelivery')}</label>
                <Controller
                  name="requestedDeliveryDate"
                  control={control}
                  render={({ field }) => (
                    <LocalizedDateInput
                      ref={field.ref}
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      locale={locale}
                      ariaLabel={t('orders.fields.requestedDelivery')}
                    />
                  )}
                />
              </div>
              <div>
                <label className={labelCls}>{t('orders.fields.promisedDelivery')}</label>
                <Controller
                  name="promisedDeliveryDate"
                  control={control}
                  render={({ field }) => (
                    <LocalizedDateInput
                      ref={field.ref}
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      locale={locale}
                      ariaLabel={t('orders.fields.promisedDelivery')}
                    />
                  )}
                />
              </div>
            </div>
          </div>

          <div className={sectionWrapperCls}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('orders.sections.notes')}</h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 sm:grid-cols-2 gap-5`}>
              <div>
                <label className={labelCls}>{t('orders.fields.customerNotes')}</label>
                <textarea
                  rows={2}
                  className={fieldCls}
                  placeholder={t('orders.fields.customerNotesPlaceholder')}
                  {...register('customerNotes')}
                />
              </div>
              <div>
                <label className={labelCls}>{t('orders.fields.internalNotes')}</label>
                <textarea
                  rows={2}
                  className={fieldCls}
                  placeholder={t('orders.fields.internalNotesPlaceholder')}
                  {...register('internalNotes')}
                />
              </div>
              <div className="col-span-1 sm:col-span-2">
                <label className={labelCls}>
                  {t('orders.fields.notes', { defaultValue: 'Genel Notlar' })}
                </label>
                <textarea
                  rows={2}
                  className={fieldCls}
                  placeholder={t('orders.fields.generalNotesPlaceholder')}
                  {...register('notes')}
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <div
        className={
          step === 2
            ? 'grid min-h-full grid-cols-1 items-stretch gap-4 pb-2 xl:grid-cols-2 2xl:grid-cols-[minmax(0,7fr)_minmax(20rem,3fr)_minmax(17rem,2fr)]'
            : 'hidden'
        }
      >
        <div className="flex min-w-0 flex-col gap-4 xl:col-span-2 2xl:col-span-1">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-200">
                {t('orders.tabs.lines')}
              </h2>
            </div>
            <button
              type="button"
              disabled={!isDraft}
              onClick={addLine}
              className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-sm font-medium rounded-md transition-colors flex items-center gap-1.5 shadow-sm shadow-indigo-500/20"
            >
              <Plus size={16} />
              {t('orders.lines.add')}
            </button>
          </div>

          <DocumentLineTable
            headerGridCls={LINE_HEADER_GRID_CLS}
            error={errors.lines?.message ? translateError(errors.lines.message) : undefined}
            header={
              <>
                <div>{t('orders.lines.product')}</div>
                <div className="grid min-w-0 grid-cols-[minmax(0,0.7fr)_minmax(0,1.2fr)_minmax(0,0.6fr)_minmax(0,0.75fr)] gap-2">
                  <div className="text-right">{t('orders.lines.quantity')}</div>
                  <div className="text-right">{t('orders.lines.unitPrice')}</div>
                  <div className="text-right">{t('orders.lines.discountPercent')}</div>
                  <div className="text-right">{t('orders.lines.taxRate')}</div>
                </div>
                <div aria-hidden="true" />
                <div className="text-right">{t('orders.fields.total')}</div>
              </>
            }
          >
            {fields.map((field, index) => (
              <OrderLineEditor
                key={field.id}
                index={index}
                isLast={index === fields.length - 1}
                register={register}
                errors={errors.lines?.[index]}
                line={watchedLines?.[index]}
                products={products}
                taxRates={taxRates}
                warehouses={warehouses}
                withholdingCodes={withholdingCodes}
                disabled={!isDraft}
                canRemove={fields.length > 1}
                locale={locale}
                currency={currency}
                decimals={decimals}
                setProductRef={(el) => {
                  if (el) productRefs.current.set(field.id, el);
                  else productRefs.current.delete(field.id);
                }}
                onProductSelect={handleProductSelect}
                onTaxRateSelect={handleTaxRateSelect}
                onRemove={remove}
                onAddLine={addLine}
              />
            ))}
          </DocumentLineTable>
        </div>

        <div className={`${sectionWrapperCls} min-w-0`}>
          <div className={sectionHeaderCls}>
            <h3 className={sectionTitleCls}>{t('orders.sections.commercial')}</h3>
          </div>
          <div className={`${sectionBodyCls} space-y-4`}>
            <div className="grid grid-cols-1 items-start gap-4 sm:grid-cols-2">
              <div className="min-w-0">
                <label className={labelCls}>{t('orders.fields.currency')}</label>
                <Controller
                  name="currency"
                  control={control}
                  render={({ field }) => (
                    <CurrencySelect value={field.value} onChange={field.onChange} />
                  )}
                />
              </div>
              <div className="min-w-0">
                <label className={labelCls}>{t('orders.fields.exchangeRate')}</label>
                <input
                  className={fieldCls}
                  type="number"
                  step="0.0001"
                  min="0"
                  {...register('exchangeRate')}
                />
                {fxSnapshot && (
                  <p className="mt-1 text-[10px] text-slate-500">
                    {t('orders.fx.autoRate', {
                      source: fxSnapshot.source,
                      date: new Date(fxSnapshot.effectiveDate).toLocaleDateString(locale),
                    })}
                  </p>
                )}
              </div>
            </div>
            <div className="grid grid-cols-1 items-start gap-4 sm:grid-cols-2">
              <div className="min-w-0">
                <div className="mb-1 flex items-center justify-between">
                  <label className={labelCls.replace('mb-1 ', '')}>
                    {t('orders.fields.paymentTerms')}
                  </label>
                  <button
                    type="button"
                    onClick={() => setQuickAdd('paymentTerm')}
                    className={quickAddBtnCls}
                  >
                    <Plus size={12} /> {t('common.new')}
                  </button>
                </div>
                <select className={fieldCls} {...register('paymentTermsId')}>
                  <option value="">{t('orders.lines.none')}</option>
                  {paymentTerms.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="min-w-0">
                <div className="mb-1 flex items-center justify-between">
                  <label className={labelCls.replace('mb-1 ', '')}>
                    {t('orders.fields.priceList')}
                  </label>
                  <button
                    type="button"
                    onClick={() => setQuickAdd('priceList')}
                    className={quickAddBtnCls}
                  >
                    <Plus size={12} /> {t('common.new')}
                  </button>
                </div>
                <select className={fieldCls} {...register('priceListId')}>
                  <option value="">{t('orders.lines.none')}</option>
                  {priceLists.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className={labelCls}>{t('orders.fields.headerDiscount')}</label>
                <input
                  className={fieldCls}
                  type="number"
                  step="0.01"
                  min="0"
                  max="100"
                  placeholder="0.00"
                  {...register('headerDiscountPercent')}
                />
              </div>
              <div>
                <label className={labelCls}>{t('orders.fields.shippingCost')}</label>
                <input
                  className={fieldCls}
                  type="number"
                  step="0.01"
                  min="0"
                  placeholder="0.00"
                  {...register('shippingCost')}
                />
              </div>
            </div>
            <div>
              <label className={labelCls}>{t('orders.fields.channel')}</label>
              <input
                className={fieldCls}
                placeholder={t('orders.fields.channelPlaceholder')}
                {...register('channel')}
              />
            </div>
          </div>
        </div>

        <div className="h-full min-w-0">
          <div className={`${sectionWrapperCls} h-full overflow-hidden`}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('orders.summary.financial')}</h3>
            </div>
            <div className="p-5 space-y-4">
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-400">{t('orders.summary.subtotal')}</span>
                <span className="text-slate-900 dark:text-slate-200 font-medium">
                  {formatCurrency(summary.subtotal, locale, currency, decimals)}
                </span>
              </div>
              {summary.lineDiscount > 0 && (
                <div className="flex justify-between items-center text-sm">
                  <span className="text-slate-400">
                    {summary.lineDiscountPct !== null
                      ? t('orders.summary.lineDiscountWithRate', {
                          pct: summary.lineDiscountPct,
                        })
                      : t('orders.summary.lineDiscount')}
                  </span>
                  <span className="text-red-400 font-medium">
                    - {formatCurrency(summary.lineDiscount, locale, currency, decimals)}
                  </span>
                </div>
              )}
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-400">
                  {summary.headerDiscountPct > 0
                    ? t('orders.summary.headerDiscountWithRate', {
                        pct: summary.headerDiscountPct,
                      })
                    : t('orders.summary.headerDiscount')}
                </span>
                <span className="text-slate-500">
                  - {formatCurrency(summary.headerDiscount, locale, currency, decimals)}
                </span>
              </div>
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-400">
                  {summary.taxPct !== null
                    ? t('orders.summary.taxWithRate', { pct: summary.taxPct })
                    : t('orders.summary.tax')}
                </span>
                <span className="text-slate-900 dark:text-slate-200 font-medium">
                  {formatCurrency(summary.tax, locale, currency, decimals)}
                </span>
              </div>
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-400">
                  {summary.withholdingPct !== null
                    ? t('orders.summary.withholdingWithRate', { pct: summary.withholdingPct })
                    : t('orders.summary.withholding')}
                </span>
                <span className="text-slate-500">
                  - {formatCurrency(summary.withholding, locale, currency, decimals)}
                </span>
              </div>
              <div className="flex justify-between items-center text-sm">
                <span className="text-slate-400">{t('orders.summary.shipping')}</span>
                <span className="text-slate-500">
                  {formatCurrency(summary.shipping, locale, currency, decimals)}
                </span>
              </div>

              <div className="pt-5 border-t border-slate-200 dark:border-[#2a3143] mt-2">
                <div className="flex justify-between items-end mb-1.5">
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-300">
                    {t('orders.summary.grandTotal')}
                  </span>
                  <span className="text-2xl font-bold text-slate-900 dark:text-white tracking-tight">
                    {formatCurrency(summary.grandTotal, locale, currency, decimals)}
                  </span>
                </div>
                <p className="text-[10px] text-slate-500 text-right">
                  {t('orders.summary.estimate', {
                    defaultValue: 'Estimated — calculated based on current inputs',
                  })}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </DocumentFormLayout>
  );
};
