import { useEffect, useMemo, useState } from 'react';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { LocalizedDateInput } from '@/shared/ui/form/LocalizedDateInput';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useDraftAutosave } from '@/shared/hooks/useDraftAutosave';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { formatCurrency } from '@/shared/lib/format';
import { computeDocumentTotals } from '@/shared/lib/documentTotals';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
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
import {
  useVatExemptionCodesQuery,
  useWithholdingTaxCodesQuery,
} from '@/shared/master-data/hooks/useMasterData';
import type { WithholdingTaxCode } from '@/shared/master-data/model/masterData.types';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import { useCreateStandaloneInvoice } from '@/features/invoices/hooks/useInvoiceQueries';
import {
  standaloneInvoiceSchema,
  type StandaloneInvoiceFormValues,
} from '@/features/invoices/model/standaloneInvoiceSchema';
import type { Invoice, StandaloneInvoiceLineInput } from '@/features/invoices/model/invoice.types';
import type { ApiResponse } from '@/shared/types/api';
import { StandaloneInvoiceLineEditor } from './StandaloneInvoiceLineEditor';

interface Props {
  open: boolean;
  onClose: () => void;
  onCreated?: (invoiceId: string) => void;
  presentation?: 'modal' | 'page';
}

const LINE_HEADER_GRID_CLS =
  'lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5.5rem,0.9fr)]';

const INVOICE_DRAFT_KEY = 'corealign:draft:invoice-standalone';

const truncate = (value: string, max = 40): string =>
  value.length > max ? `${value.slice(0, max)}…` : value;

const toIsoUtcMidnight = (date: string): string =>
  date ? new Date(`${date}T00:00:00Z`).toISOString() : new Date().toISOString();

const todayIso = (): string => new Date().toISOString().slice(0, 10);

const numOrUndefined = (value?: string): number | undefined =>
  value && Number(value) ? Number(value) : undefined;

const emptyLine = () => ({
  productSku: '',
  productName: '',
  description: '',
  quantity: 1,
  unitPrice: 0,
  lineDiscountPercent: '',
  taxRatePercent: '20',
  withholdingTaxCodeId: '',
});

const emptyValues = (): StandaloneInvoiceFormValues => ({
  customerId: '',
  issueDate: todayIso(),
  dueDays: 30,
  currency: 'TRY',
  headerDiscountPercent: '',
  shippingCost: '',
  vatExemptionCodeId: '',
  vatExemptionReason: '',
  publicNotes: '',
  internalNotes: '',
  lines: [emptyLine()],
});

export const CreateStandaloneInvoiceModal = ({
  open,
  onClose,
  onCreated,
  presentation = 'modal',
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = useFormatLocale();
  const decimals = useDecimalPlaces();
  const createMutation = useCreateStandaloneInvoice();

  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const withholdingCodesQuery = useWithholdingTaxCodesQuery(true);
  const vatExemptionCodesQuery = useVatExemptionCodesQuery(true);

  const customers = customersQuery.data?.data?.items ?? [];
  const withholdingCodes = withholdingCodesQuery.data?.data ?? [];
  const vatExemptionCodes = vatExemptionCodesQuery.data?.data ?? [];

  const {
    register,
    control,
    handleSubmit,
    reset,
    trigger,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<StandaloneInvoiceFormValues>({
    resolver: zodResolver(standaloneInvoiceSchema),
    defaultValues: emptyValues(),
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, open);
  const backdrop = useBackdropClick(requestClose);

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  const allValues = useWatch({ control }) as StandaloneInvoiceFormValues;
  const draft = useDraftAutosave<StandaloneInvoiceFormValues>(INVOICE_DRAFT_KEY, allValues, {
    enabled: open && isDirty,
  });

  const [step, setStep] = useState<1 | 2>(1);
  const [draftToRestore, setDraftToRestore] = useState<StandaloneInvoiceFormValues | null>(null);
  // WHY seeded false and not `open`: the page route mounts with open already true, so seeding
  // from the prop makes this branch unreachable there and the saved draft is never offered back.
  const [seenOpen, setSeenOpen] = useState(false);
  if (open !== seenOpen) {
    setSeenOpen(open);
    setStep(1);
    setDraftToRestore(open ? draft.peekDraft() : null);
  }

  useEffect(() => {
    if (open) reset(emptyValues());
  }, [open, reset]);

  const handleStepClick = async (targetStep: 1 | 2) => {
    if (targetStep === 2 && step === 1) {
      const isValid = await trigger(['customerId', 'issueDate', 'dueDays', 'currency']);
      if (!isValid) return;
    }
    setStep(targetStep);
  };

  const watchedLines = useWatch({ control, name: 'lines' });
  const watchedCurrency = useWatch({ control, name: 'currency' });
  const watchedHeaderDiscount = useWatch({ control, name: 'headerDiscountPercent' });
  const watchedShipping = useWatch({ control, name: 'shippingCost' });
  const watchedIssueDate = useWatch({ control, name: 'issueDate' });
  const watchedDueDays = useWatch({ control, name: 'dueDays' });
  const watchedExemptionCodeId = useWatch({ control, name: 'vatExemptionCodeId' });

  const currency = (watchedCurrency || 'TRY').toUpperCase();

  const withholdingCodeById = useMemo(() => {
    const m = new Map<string, WithholdingTaxCode>();
    for (const c of withholdingCodesQuery.data?.data ?? []) m.set(c.id, c);
    return m;
  }, [withholdingCodesQuery.data]);

  const summary = useMemo(
    () =>
      computeDocumentTotals({
        lines: (watchedLines ?? []).map((l) => ({
          productId: l.productSku,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          lineDiscountPercent: l.lineDiscountPercent,
          taxRatePercent: l.taxRatePercent,
          withholdingTaxCodeId: l.withholdingTaxCodeId,
        })),
        headerDiscountPercent: watchedHeaderDiscount,
        shippingCost: watchedShipping,
        withholdingCodeById,
      }),
    [watchedLines, watchedHeaderDiscount, watchedShipping, withholdingCodeById],
  );

  const dueDatePreview = useMemo(() => {
    if (!watchedIssueDate) return '';
    const base = new Date(`${watchedIssueDate}T00:00:00Z`);
    if (Number.isNaN(base.getTime())) return '';
    base.setUTCDate(base.getUTCDate() + (Number.isFinite(watchedDueDays) ? watchedDueDays : 0));
    try {
      return new Intl.DateTimeFormat(i18n.language, {
        dateStyle: 'medium',
        timeZone: 'UTC',
      }).format(base);
    } catch {
      return base.toISOString().slice(0, 10);
    }
  }, [watchedIssueDate, watchedDueDays, i18n.language]);

  const onSubmit = handleSubmit(
    (values) => {
      const lines: StandaloneInvoiceLineInput[] = values.lines.map((l) => ({
        productId: null,
        productSku: l.productSku.trim(),
        productName: l.productName.trim(),
        description: l.description?.trim() || null,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        taxRatePercent: numOrUndefined(l.taxRatePercent) ?? 0,
        lineDiscountPercent: numOrUndefined(l.lineDiscountPercent) ?? null,
        withholdingTaxCodeId: l.withholdingTaxCodeId || null,
      }));

      createMutation.mutate(
        {
          customerId: values.customerId,
          issueDate: toIsoUtcMidnight(values.issueDate),
          dueDays: values.dueDays,
          currency: values.currency.toUpperCase(),
          headerDiscountPercent: numOrUndefined(values.headerDiscountPercent) ?? null,
          shippingCost: numOrUndefined(values.shippingCost) ?? null,
          vatExemptionCodeId: values.vatExemptionCodeId || null,
          vatExemptionReason:
            values.vatExemptionCodeId && values.vatExemptionReason?.trim()
              ? values.vatExemptionReason.trim()
              : null,
          publicNotes: values.publicNotes?.trim() || null,
          internalNotes: values.internalNotes?.trim() || null,
          lines,
        },
        {
          onSuccess: (response: ApiResponse<Invoice>) => {
            if (response.isSuccess && response.data) {
              toast.success(t('invoices.standalone.created'));
              draft.clearDraft();
              onCreated?.(response.data.id);
              reset(emptyValues());
              onClose();
              return;
            }
            toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
          },
          onError: (error: unknown) => toastApiError(error, t('auth.common.unexpectedError')),
        },
      );
    },
    (formErrors) => {
      setStep(Object.keys(formErrors).some((k) => k !== 'lines') ? 1 : 2);
    },
  );

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (!open) return null;

  const isBusy = isSubmitting || createMutation.isPending;
  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  const stepNavigation = (
    <FormWizardSteps
      steps={[
        { id: 1, label: t('invoices.standalone.tabs.info') },
        { id: 2, label: t('invoices.standalone.tabs.lines') },
      ]}
      current={step}
      onSelect={(id) => void handleStepClick(id as 1 | 2)}
      ariaLabel={t('invoices.standalone.title')}
    />
  );

  const footer = (
    <>
      <div className="text-sm">
        <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('invoices.standalone.summary.grandTotal')}
        </span>{' '}
        <span className="font-semibold text-slate-900 dark:text-slate-100">
          {formatCurrency(summary.grandTotal, locale, currency, decimals)}
        </span>
        {draft.lastSavedAt && (
          <div className="text-[10px] text-slate-400 dark:text-slate-500">
            {t('invoices.standalone.draft.savedAt', {
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
            {t('invoices.standalone.create')}
          </Button>
        )}
      </div>
    </>
  );

  return (
    <DocumentFormLayout
      presentation={presentation}
      title={t('invoices.standalone.title')}
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
          <span className="text-primary-800 dark:text-primary-200">
            {t('invoices.standalone.draft.found')}
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
              {t('invoices.standalone.draft.restore')}
            </button>
            <button
              type="button"
              onClick={() => {
                draft.clearDraft();
                setDraftToRestore(null);
              }}
              className="rounded px-2 py-1 font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              {t('invoices.standalone.draft.discard')}
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
              <h3 className={sectionTitleCls}>{t('invoices.standalone.sections.general')}</h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 gap-5 sm:grid-cols-2`}>
              <div className="col-span-1 sm:col-span-2">
                <label className={labelCls}>{t('invoices.standalone.customer')}</label>
                <select
                  className={fieldCls}
                  aria-label={t('invoices.standalone.customer')}
                  {...register('customerId')}
                >
                  <option value="">{t('invoices.standalone.selectCustomer')}</option>
                  {customers.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name} {c.code ? `(${c.code})` : ''}
                    </option>
                  ))}
                </select>
                {errors.customerId?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.customerId.message)}
                  </span>
                )}
              </div>

              <div>
                <label className={labelCls}>{t('invoices.standalone.issueDate')}</label>
                <Controller
                  name="issueDate"
                  control={control}
                  render={({ field }) => (
                    <LocalizedDateInput
                      ref={field.ref}
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      locale={locale}
                      ariaLabel={t('invoices.standalone.issueDate')}
                    />
                  )}
                />
                {errors.issueDate?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.issueDate.message)}
                  </span>
                )}
              </div>

              <div>
                <label className={labelCls}>{t('invoices.standalone.dueDays')}</label>
                <input
                  className={fieldCls}
                  type="number"
                  min="0"
                  max="365"
                  step="1"
                  aria-label={t('invoices.standalone.dueDays')}
                  {...register('dueDays', { valueAsNumber: true })}
                />
                {errors.dueDays?.message ? (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.dueDays.message)}
                  </span>
                ) : (
                  dueDatePreview && (
                    <p className="mt-1 text-[10px] text-primary-600 dark:text-primary-300">
                      {t('invoices.standalone.duePreview', {
                        days: watchedDueDays,
                        date: dueDatePreview,
                      })}
                    </p>
                  )
                )}
              </div>

              <div className="col-span-1 sm:col-span-2">
                <label className={labelCls}>{t('invoices.standalone.currency')}</label>
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
            </div>
          </div>

          <div className={sectionWrapperCls}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('invoices.standalone.sections.exemption')}</h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 gap-5`}>
              <div>
                <label className={labelCls}>{t('invoices.standalone.exemptionCode')}</label>
                <select
                  className={fieldCls}
                  aria-label={t('invoices.standalone.exemptionCode')}
                  {...register('vatExemptionCodeId')}
                >
                  <option value="">{t('invoices.standalone.exemptionCodePlaceholder')}</option>
                  {vatExemptionCodes.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.code} — {truncate(c.name)}
                    </option>
                  ))}
                </select>
              </div>
              {watchedExemptionCodeId && (
                <div>
                  <label className={labelCls}>{t('invoices.standalone.exemptionReason')}</label>
                  <input
                    className={fieldCls}
                    aria-label={t('invoices.standalone.exemptionReason')}
                    {...register('vatExemptionReason')}
                  />
                </div>
              )}
            </div>
          </div>

          <div className={sectionWrapperCls}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('invoices.standalone.sections.notes')}</h3>
            </div>
            <div className={`${sectionBodyCls} grid grid-cols-1 gap-5 sm:grid-cols-2`}>
              <div>
                <label className={labelCls}>{t('invoices.standalone.publicNotes')}</label>
                <textarea
                  rows={2}
                  className={fieldCls}
                  aria-label={t('invoices.standalone.publicNotes')}
                  {...register('publicNotes')}
                />
              </div>
              <div>
                <label className={labelCls}>{t('invoices.standalone.internalNotes')}</label>
                <textarea
                  rows={2}
                  className={fieldCls}
                  aria-label={t('invoices.standalone.internalNotes')}
                  {...register('internalNotes')}
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
            <h2 className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-200">
              {t('invoices.standalone.tabs.lines')}
            </h2>
            <button
              type="button"
              onClick={() => append(emptyLine())}
              className="flex items-center gap-1.5 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm shadow-indigo-500/20 transition-colors hover:bg-indigo-500"
            >
              <Plus size={16} />
              {t('invoices.standalone.addLine')}
            </button>
          </div>

          <DocumentLineTable
            headerGridCls={LINE_HEADER_GRID_CLS}
            error={errors.lines?.message ? translateError(errors.lines.message) : undefined}
            header={
              <>
                <div>{t('invoices.standalone.lineName')}</div>
                <div className="grid min-w-0 grid-cols-[minmax(0,0.7fr)_minmax(0,1.2fr)_minmax(0,0.6fr)_minmax(0,0.75fr)] gap-2">
                  <div className="text-right">{t('invoices.standalone.lineQuantity')}</div>
                  <div className="text-right">{t('invoices.standalone.lineUnitPrice')}</div>
                  <div className="text-right">{t('invoices.standalone.lineDiscountPercent')}</div>
                  <div className="text-right">{t('invoices.standalone.lineTaxRate')}</div>
                </div>
                <div aria-hidden="true" />
                <div className="text-right">{t('invoices.standalone.summary.lineTotal')}</div>
              </>
            }
          >
            {fields.map((field, index) => (
              <StandaloneInvoiceLineEditor
                key={field.id}
                index={index}
                register={register}
                errors={errors.lines?.[index]}
                line={watchedLines?.[index]}
                withholdingCodes={withholdingCodes}
                canRemove={fields.length > 1}
                locale={locale}
                currency={currency}
                decimals={decimals}
                onRemove={remove}
              />
            ))}
          </DocumentLineTable>
        </div>

        <div className={`${sectionWrapperCls} min-w-0`}>
          <div className={sectionHeaderCls}>
            <h3 className={sectionTitleCls}>{t('invoices.standalone.sections.commercial')}</h3>
          </div>
          <div className={`${sectionBodyCls} space-y-4`}>
            <div>
              <label className={labelCls}>{t('invoices.standalone.headerDiscount')}</label>
              <input
                className={fieldCls}
                type="number"
                step="0.01"
                min="0"
                max="100"
                placeholder="0.00"
                aria-label={t('invoices.standalone.headerDiscount')}
                {...register('headerDiscountPercent')}
              />
            </div>
            <div>
              <label className={labelCls}>{t('invoices.standalone.shippingCost')}</label>
              <input
                className={fieldCls}
                type="number"
                step="0.01"
                min="0"
                placeholder="0.00"
                aria-label={t('invoices.standalone.shippingCost')}
                {...register('shippingCost')}
              />
            </div>
          </div>
        </div>

        <div className="h-full min-w-0">
          <div className={`${sectionWrapperCls} h-full overflow-hidden`}>
            <div className={sectionHeaderCls}>
              <h3 className={sectionTitleCls}>{t('invoices.standalone.sections.summary')}</h3>
            </div>
            <div className="space-y-4 p-5">
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">{t('invoices.standalone.summary.subtotal')}</span>
                <span className="font-medium text-slate-900 dark:text-slate-200">
                  {formatCurrency(summary.subtotal, locale, currency, decimals)}
                </span>
              </div>
              {summary.lineDiscount > 0 && (
                <div className="flex items-center justify-between text-sm">
                  <span className="text-slate-400">
                    {t('invoices.standalone.summary.lineDiscount')}
                  </span>
                  <span className="font-medium text-red-400">
                    - {formatCurrency(summary.lineDiscount, locale, currency, decimals)}
                  </span>
                </div>
              )}
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">
                  {summary.headerDiscountPct > 0
                    ? t('invoices.standalone.summary.headerDiscountWithRate', {
                        pct: summary.headerDiscountPct,
                      })
                    : t('invoices.standalone.summary.headerDiscount')}
                </span>
                <span className="text-slate-500">
                  - {formatCurrency(summary.headerDiscount, locale, currency, decimals)}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">
                  {summary.taxPct !== null
                    ? t('invoices.standalone.summary.taxWithRate', { pct: summary.taxPct })
                    : t('invoices.standalone.summary.tax')}
                </span>
                <span className="font-medium text-slate-900 dark:text-slate-200">
                  {formatCurrency(summary.tax, locale, currency, decimals)}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">
                  {t('invoices.standalone.summary.withholding')}
                </span>
                <span className="text-slate-500">
                  - {formatCurrency(summary.withholding, locale, currency, decimals)}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">{t('invoices.standalone.summary.shipping')}</span>
                <span className="text-slate-500">
                  {formatCurrency(summary.shipping, locale, currency, decimals)}
                </span>
              </div>

              <div className="mt-2 border-t border-slate-200 pt-5 dark:border-[#2a3143]">
                <div className="mb-1.5 flex items-end justify-between">
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-300">
                    {t('invoices.standalone.summary.grandTotal')}
                  </span>
                  <span className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
                    {formatCurrency(summary.grandTotal, locale, currency, decimals)}
                  </span>
                </div>
                <p className="text-right text-[10px] text-slate-500">
                  {t('invoices.standalone.summary.estimate')}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </DocumentFormLayout>
  );
};
