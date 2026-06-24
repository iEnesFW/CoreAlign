import { useEffect, useMemo, useRef, useState } from 'react';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { ModalTabs } from '@/shared/ui/ModalTabs/ModalTabs';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { MasterDataQuickModal } from '@/shared/master-data/ui/MasterDataQuickModal';
import {
  useCustomersQuery,
  useCustomerAddressesQuery,
} from '@/features/customers/hooks/useCustomerQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import {
  usePaymentTermsQuery,
  usePriceListsQuery,
  useTaxRatesQuery,
  useUomsQuery,
  useWarehousesQuery,
} from '@/shared/master-data/hooks/useMasterData';
import { orderSchema, type OrderFormValues } from '../model/orderSchema';
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
];

const todayIso = () => new Date().toISOString().slice(0, 10);

const fieldCls =
  'w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';
const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';
const sectionCls = 'space-y-3 border-t border-slate-200 pt-3 dark:border-slate-800';
const sectionTitleCls = 'text-xs font-semibold uppercase tracking-wider text-slate-500';
const quickAddBtnCls =
  'inline-flex items-center gap-0.5 rounded px-1.5 py-0.5 text-[10px] font-medium text-primary-600 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-500/10';

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

export const OrderFormModal = ({ open, order, onClose }: Props) => {
  const { t, i18n } = useTranslation();
  const createMutation = useCreateOrder();
  const updateMutation = useUpdateOrder();
  const isEdit = order !== null;
  const isDraft = !isEdit || order?.status === 'Draft';

  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const taxRatesQuery = useTaxRatesQuery(true);
  const warehousesQuery = useWarehousesQuery(true);
  const paymentTermsQuery = usePaymentTermsQuery(true);
  const priceListsQuery = usePriceListsQuery(true);
  const uomsQuery = useUomsQuery(true);
  const decimals = useDecimalPlaces();

  const customers = customersQuery.data?.data?.items ?? [];
  const products = productsQuery.data?.data?.items ?? [];
  const taxRates = taxRatesQuery.data?.data ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];
  const paymentTerms = paymentTermsQuery.data?.data ?? [];
  const priceLists = priceListsQuery.data?.data ?? [];
  const uoms = uomsQuery.data?.data ?? [];

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(orderSchema),
    defaultValues: emptyValues,
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, open);
  const linesHaveError = !!errors.lines;
  const infoHasError = Object.keys(errors).some((k) => k !== 'lines');

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  const [quickAdd, setQuickAdd] = useState<'paymentTerm' | 'priceList' | null>(null);
  const [tab, setTab] = useState<'info' | 'lines'>('info');

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
    append({ productId: '', quantity: 1, unitPrice: 0 });
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
          warehouseId: l.warehouseId ?? '',
          lineNotes: l.lineNotes ?? '',
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

  const addressesQuery = useCustomerAddressesQuery(watchedCustomerId || null);
  const addresses = addressesQuery.data?.data ?? [];

  const currency = (watchedCurrency || 'USD').toUpperCase();
  const locale = i18n.language;

  const summary = useMemo(() => {
    const lines = watchedLines ?? [];
    let subtotal = 0;
    let lineDiscount = 0;
    let tax = 0;
    let withholding = 0;
    for (const l of lines) {
      const gross = (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0);
      const disc = gross * ((Number(l.lineDiscountPercent) || 0) / 100);
      const net = gross - disc;
      subtotal += gross;
      lineDiscount += disc;
      tax += net * ((Number(l.taxRatePercent) || 0) / 100);
      withholding += net * ((Number(l.withholdingRatePercent) || 0) / 100);
    }
    const afterLineDiscount = subtotal - lineDiscount;
    const headerDiscount = afterLineDiscount * ((Number(watchedHeaderDiscount) || 0) / 100);
    const taxableTotal = afterLineDiscount - headerDiscount;
    const shipping = Number(watchedShipping) || 0;
    const grandTotal = taxableTotal + tax - withholding + shipping;
    return {
      subtotal,
      lineDiscount,
      headerDiscount,
      taxableTotal,
      tax,
      withholding,
      shipping,
      grandTotal,
    };
  }, [watchedLines, watchedHeaderDiscount, watchedShipping]);

  const handleProductSelect = (index: number, productId: string) => {
    setValue(`lines.${index}.productId`, productId, { shouldValidate: true });
    const product = products.find((p) => p.id === productId);
    if (!product) return;
    setValue(`lines.${index}.unitPrice`, product.price);
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
      const lines: OrderLineInput[] = values.lines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        uomId: l.uomId || null,
        uomCode: l.uomCode || null,
        lineDiscountPercent: numOrUndefined(l.lineDiscountPercent),
        taxRateId: l.taxRateId || null,
        taxRatePercent: numOrUndefined(l.taxRatePercent),
        withholdingRatePercent: numOrUndefined(l.withholdingRatePercent),
        warehouseId: l.warehouseId || null,
        lineNotes: l.lineNotes || null,
      }));

      const payload = {
        orderNumber: values.orderNumber,
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
            onClose();
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      });
    },
    (formErrors) => {
      setTab(Object.keys(formErrors).some((k) => k !== 'lines') ? 'info' : 'lines');
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
      onClick={requestClose}
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
              id: 'info',
              label: t('orders.tabs.info', { defaultValue: 'Genel Bilgiler' }),
              hasError: infoHasError,
            },
            {
              id: 'lines',
              label: t('orders.tabs.lines', { defaultValue: 'Kalemler' }),
              badge: fields.length,
              hasError: linesHaveError,
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
          <div className={tab === 'info' ? 'space-y-4' : 'hidden'}>
            <section className="space-y-3">
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
                  <span className="mt-1 block text-xs text-danger-500">
                    {translateError(errors.customerId.message)}
                  </span>
                )}
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className={labelCls}>{t('orders.fields.type')}</label>
                  <select disabled={!isDraft} className={fieldCls} {...register('type')}>
                    {ORDER_TYPES.map((ty) => (
                      <option key={ty} value={ty}>
                        {t(`orders.type.${ty}` as never)}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className={labelCls}>{t('orders.fields.source')}</label>
                  <select disabled={!isDraft} className={fieldCls} {...register('source')}>
                    {ORDER_SOURCES.map((s) => (
                      <option key={s} value={s}>
                        {t(`orders.source.${s}` as never)}
                      </option>
                    ))}
                  </select>
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
                </div>
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('orders.sections.commercial')}</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className={labelCls}>{t('orders.fields.currency')}</label>
                  <Controller
                    name="currency"
                    control={control}
                    render={({ field }) => (
                      <CurrencySelect value={field.value} onChange={field.onChange} />
                    )}
                  />
                </div>
                <Input
                  label={t('orders.fields.exchangeRate')}
                  type="number"
                  step="0.0001"
                  min="0"
                  {...register('exchangeRate')}
                />
                <div>
                  <div className="mb-1 flex items-center justify-between">
                    <label className="text-xs font-medium text-slate-700 dark:text-slate-300">
                      {t('orders.fields.paymentTerms')}
                    </label>
                    <button
                      type="button"
                      onClick={() => setQuickAdd('paymentTerm')}
                      className={quickAddBtnCls}
                    >
                      <Plus size={11} /> Yeni
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
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <div className="mb-1 flex items-center justify-between">
                    <label className="text-xs font-medium text-slate-700 dark:text-slate-300">
                      {t('orders.fields.priceList')}
                    </label>
                    <button
                      type="button"
                      onClick={() => setQuickAdd('priceList')}
                      className={quickAddBtnCls}
                    >
                      <Plus size={11} /> Yeni
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
                <Input
                  label={t('orders.fields.headerDiscount')}
                  type="number"
                  step="0.01"
                  min="0"
                  max="100"
                  {...register('headerDiscountPercent')}
                />
                <Input
                  label={t('orders.fields.shippingCost')}
                  type="number"
                  step="0.01"
                  min="0"
                  {...register('shippingCost')}
                />
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <Input
                  label={t('orders.fields.requestedDelivery')}
                  type="date"
                  {...register('requestedDeliveryDate')}
                />
                <Input
                  label={t('orders.fields.promisedDelivery')}
                  type="date"
                  {...register('promisedDeliveryDate')}
                />
                <Input label={t('orders.fields.channel')} {...register('channel')} />
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('orders.sections.addresses')}</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
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
                        {a.label} — {a.line1}
                        {a.city ? `, ${a.city}` : ''}
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
                        {a.label} — {a.line1}
                        {a.city ? `, ${a.city}` : ''}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </section>

            <section className={sectionCls}>
              <h3 className={sectionTitleCls}>{t('orders.sections.notes')}</h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <label className={labelCls}>{t('orders.fields.customerNotes')}</label>
                  <textarea rows={2} className={fieldCls} {...register('customerNotes')} />
                </div>
                <div>
                  <label className={labelCls}>{t('orders.fields.internalNotes')}</label>
                  <textarea rows={2} className={fieldCls} {...register('internalNotes')} />
                </div>
              </div>
              <div>
                <label className={labelCls}>{t('orders.fields.notes')}</label>
                <textarea
                  rows={2}
                  placeholder={t('orders.fields.notesPlaceholder')}
                  className={fieldCls}
                  {...register('notes')}
                />
              </div>
            </section>
          </div>

          <div className={tab === 'lines' ? 'space-y-4' : 'hidden'}>
            <section className={sectionCls}>
              <div className="flex items-center justify-between">
                <h3 className={sectionTitleCls}>{t('orders.fields.lines')}</h3>
                <button
                  type="button"
                  disabled={!isDraft}
                  onClick={addLine}
                  className="inline-flex items-center gap-1 rounded bg-primary-50 px-2 py-1 text-xs font-medium text-primary-700 hover:bg-primary-100 disabled:opacity-50 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-500/20"
                >
                  <Plus size={12} />
                  {t('orders.lines.add')}
                </button>
              </div>

              {errors.lines?.message && (
                <div className="text-xs text-danger-500">
                  {translateError(errors.lines.message)}
                </div>
              )}

              <div className="space-y-2">
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
              </div>

              <div className="rounded border border-slate-200 bg-slate-50 p-3 text-sm dark:border-slate-700 dark:bg-slate-800/50">
                <dl className="space-y-1">
                  <SummaryRow
                    label={t('orders.summary.subtotal')}
                    value={formatCurrency(summary.subtotal, locale, currency, decimals)}
                  />
                  <SummaryRow
                    label={t('orders.summary.lineDiscount')}
                    value={`- ${formatCurrency(summary.lineDiscount, locale, currency, decimals)}`}
                  />
                  <SummaryRow
                    label={t('orders.summary.headerDiscount')}
                    value={`- ${formatCurrency(summary.headerDiscount, locale, currency, decimals)}`}
                  />
                  <SummaryRow
                    label={t('orders.summary.tax')}
                    value={formatCurrency(summary.tax, locale, currency, decimals)}
                  />
                  <SummaryRow
                    label={t('orders.summary.withholding')}
                    value={`- ${formatCurrency(summary.withholding, locale, currency, decimals)}`}
                  />
                  <SummaryRow
                    label={t('orders.summary.shipping')}
                    value={formatCurrency(summary.shipping, locale, currency, decimals)}
                  />
                  <div className="mt-1 flex justify-between border-t border-slate-200 pt-1 font-semibold text-slate-900 dark:border-slate-700 dark:text-slate-100">
                    <dt>{t('orders.summary.grandTotal')}</dt>
                    <dd>{formatCurrency(summary.grandTotal, locale, currency, decimals)}</dd>
                  </div>
                </dl>
                <p className="mt-2 text-[11px] text-slate-400">{t('orders.summary.estimate')}</p>
              </div>
            </section>
          </div>

          <div className="sticky bottom-0 flex items-center justify-between gap-2 border-t border-slate-200 bg-white pt-3 dark:border-slate-800 dark:bg-slate-900">
            <div className="text-sm">
              <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('orders.summary.grandTotal')}
              </span>{' '}
              <span className="font-semibold text-slate-900 dark:text-slate-100">
                {formatCurrency(summary.grandTotal, locale, currency, decimals)}
              </span>
            </div>
            <div className="flex gap-2">
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
          </div>
        </form>
      </div>

      {quickAdd && (
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
      )}
    </div>
  );
};

const SummaryRow = ({ label, value }: { label: string; value: string }) => (
  <div className="flex justify-between text-slate-600 dark:text-slate-300">
    <dt>{label}</dt>
    <dd>{value}</dd>
  </div>
);
