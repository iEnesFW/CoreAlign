import { useEffect, useMemo, useRef } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { FileMinus } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import { toastApiError } from '@/shared/lib/mutationToast';
import { newOperationId } from '@/shared/lib/operationId';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { DocumentFormLayout } from '@/shared/ui/document-form/DocumentFormLayout';
import { DocumentLineTable } from '@/shared/ui/document-form/DocumentLineTable';
import {
  documentFieldCls as fieldCls,
  documentLabelCls as labelCls,
  documentSectionBodyCls as sectionBodyCls,
  documentSectionHeaderCls as sectionHeaderCls,
  documentSectionTitleCls as sectionTitleCls,
  documentSectionWrapperCls as sectionWrapperCls,
} from '@/shared/ui/document-form/documentFormClasses';
import {
  useCreditedQuantitiesByLine,
  useIssueCreditNote,
} from '@/features/invoices/hooks/useInvoiceQueries';
import {
  issueCreditNoteSchema,
  type IssueCreditNoteFormValues,
} from '@/features/invoices/model/issueCreditNoteSchema';
import type { Invoice } from '@/features/invoices/model/invoice.types';

interface Props {
  invoice: Invoice | null;
  open: boolean;
  onClose: () => void;
  onSuccess?: (creditNoteId: string) => void;
}

const LINE_HEADER_GRID_CLS =
  'lg:grid-cols-[2.25rem_minmax(0,3fr)_minmax(4.5rem,0.8fr)_minmax(4.5rem,0.8fr)_minmax(6rem,1fr)_minmax(6rem,1fr)_minmax(6rem,1fr)]';

const EMPTY_VALUES: IssueCreditNoteFormValues = { reason: '', lines: [] };

export const IssueCreditNoteModal = ({ invoice, open, onClose, onSuccess }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const issueMutation = useIssueCreditNote();
  const activeInvoiceId = open && invoice ? invoice.id : null;
  const creditedQuery = useCreditedQuantitiesByLine(activeInvoiceId);

  // WHY the caps are not assumed when the query fails: falling back to the full invoiced quantity
  // would offer a line that was already credited, so the operator would only learn about it from
  // the server's rejection. Without the caps the form stays closed for business.
  const capsUnavailable = creditedQuery.isError;
  const capsReady = activeInvoiceId === null || creditedQuery.isSuccess;

  const defaults = useMemo<IssueCreditNoteFormValues>(() => {
    if (!invoice || !creditedQuery.isSuccess) return EMPTY_VALUES;
    const credited = new Map<string, number>();
    for (const row of creditedQuery.data?.data ?? []) {
      credited.set(row.invoiceLineId, row.creditedQuantity);
    }
    return {
      reason: '',
      lines: (invoice.lines ?? []).map((line) => {
        const remaining = Math.max(0, line.quantity - (credited.get(line.id) ?? 0));
        return {
          invoiceLineId: line.id,
          selected: false,
          quantity: remaining,
          remaining,
        };
      }),
    };
  }, [invoice, creditedQuery.isSuccess, creditedQuery.data]);

  const {
    register,
    control,
    handleSubmit,
    reset,
    getValues,
    setValue,
    formState: { errors, isDirty },
  } = useForm<IssueCreditNoteFormValues>({
    resolver: zodResolver(issueCreditNoteSchema),
    defaultValues: EMPTY_VALUES,
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, open);
  const backdrop = useBackdropClick(requestClose);
  const { fields } = useFieldArray({ control, name: 'lines' });
  const watchedLines = useWatch({ control, name: 'lines' });

  // WHY the caps are re-applied instead of seeded once: a refetch can lower a line's remaining
  // quantity after another credit note landed, and a stale cap would send the operator into a
  // server rejection. keepDirtyValues protects the edits already made; the clamp only pulls down.
  const appliedSignature = useRef<string | null>(null);
  useEffect(() => {
    const signature =
      activeInvoiceId === null
        ? null
        : `${activeInvoiceId}|${defaults.lines.map((l) => `${l.invoiceLineId}:${l.remaining}`).join(',')}`;
    if (appliedSignature.current === signature) return;
    appliedSignature.current = signature;
    if (signature === null) {
      reset(EMPTY_VALUES);
      return;
    }
    reset(defaults, { keepDirtyValues: true });
    getValues('lines').forEach((line, index) => {
      if (line.quantity > line.remaining) {
        setValue(`lines.${index}.quantity`, line.remaining, { shouldValidate: true });
      }
    });
  }, [activeInvoiceId, defaults, reset, getValues, setValue]);

  const summary = useMemo(() => {
    const lines = invoice?.lines ?? [];
    return (watchedLines ?? []).reduce((sum, sel, index) => {
      if (!sel.selected || !(sel.quantity > 0)) return sum;
      return sum + sel.quantity * (lines[index]?.unitPrice ?? 0);
    }, 0);
  }, [watchedLines, invoice]);

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const onSubmit = handleSubmit(async (values) => {
    if (!invoice) return;
    const lines = values.lines
      .filter((l) => l.selected && l.quantity > 0)
      .map((l) => ({ invoiceLineId: l.invoiceLineId, quantity: l.quantity }));

    try {
      const response = await issueMutation.mutateAsync({
        id: invoice.id,
        payload: {
          lines,
          reason: values.reason?.trim() || null,
          operationId: newOperationId(),
        },
      });
      const newId = response.data?.id;
      toast.success(t('invoices.creditNote.toastSuccess'));
      if (newId && onSuccess) onSuccess(newId);
      onClose();
    } catch (error) {
      toastApiError(error, t('invoices.creditNote.toastError'));
    }
  });

  if (!invoice || !open) return null;

  const footer = (
    <>
      <div className="text-sm">
        <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('invoices.creditNote.subtotal')}
        </span>{' '}
        <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
          {formatCurrency(summary, locale, invoice.currency)}
        </span>
      </div>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={requestClose}
          className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-500 transition-colors hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
        >
          {t('common.cancel')}
        </button>
        <Button type="submit" isLoading={issueMutation.isPending} disabled={!capsReady}>
          <FileMinus size={14} />
          {t('invoices.creditNote.submit')}
        </Button>
      </div>
    </>
  );

  return (
    <DocumentFormLayout
      presentation="modal"
      title={t('invoices.creditNote.title')}
      closeAriaLabel={t('common.cancel')}
      onRequestClose={requestClose}
      backdropProps={backdrop}
      onSubmit={onSubmit}
      footer={footer}
    >
      <div className="flex flex-col gap-4">
        <div className={sectionWrapperCls}>
          <div className={sectionHeaderCls}>
            <h3 className={sectionTitleCls}>
              {t('invoices.creditNote.subtitle', { number: invoice.invoiceNumber })}
            </h3>
          </div>
          <div className={sectionBodyCls}>
            <p className="mb-4 text-xs text-slate-500 dark:text-slate-400">
              {t('invoices.creditNote.help')}
            </p>
            <label className={labelCls}>{t('invoices.creditNote.reason')}</label>
            <textarea
              rows={3}
              className={fieldCls}
              maxLength={500}
              aria-label={t('invoices.creditNote.reason')}
              placeholder={t('invoices.creditNote.reasonPlaceholder')}
              {...register('reason')}
            />
            {errors.reason?.message && (
              <span className="mt-1 block text-[10px] text-danger-500">
                {translateError(errors.reason.message)}
              </span>
            )}
          </div>
        </div>

        {capsUnavailable ? (
          <div className="rounded-xl border border-danger-300 bg-danger-50 px-4 py-6 text-center text-xs text-danger-700 dark:border-danger-500/40 dark:bg-danger-500/10 dark:text-danger-300">
            <p>{t('invoices.creditNote.capsUnavailable')}</p>
            <button
              type="button"
              onClick={() => void creditedQuery.refetch()}
              className="mt-2 rounded-md border border-danger-300 px-3 py-1 font-semibold transition-colors hover:bg-danger-100 dark:border-danger-500/40 dark:hover:bg-danger-500/20"
            >
              {t('common.retry')}
            </button>
          </div>
        ) : !capsReady ? (
          <div className={`${sectionWrapperCls} px-4 py-8 text-center text-xs text-slate-500`}>
            {t('common.loading')}
          </div>
        ) : (
          <DocumentLineTable
            headerGridCls={LINE_HEADER_GRID_CLS}
            error={
              errors.lines?.root?.message
                ? translateError(errors.lines.root.message)
                : errors.lines?.message
                  ? translateError(errors.lines.message)
                  : undefined
            }
            header={
              <>
                <div aria-hidden="true" />
                <div>{t('invoices.creditNote.product')}</div>
                <div className="text-right">{t('invoices.creditNote.invoiced')}</div>
                <div className="text-right">{t('invoices.creditNote.remaining')}</div>
                <div className="text-right">{t('invoices.creditNote.quantity')}</div>
                <div className="text-right">{t('invoices.creditNote.unitPrice')}</div>
                <div className="text-right">{t('invoices.creditNote.subtotal')}</div>
              </>
            }
          >
            {fields.map((field, index) => {
              const line = invoice.lines?.[index];
              const remaining = watchedLines?.[index]?.remaining ?? 0;
              const selected = watchedLines?.[index]?.selected ?? false;
              const quantity = Number(watchedLines?.[index]?.quantity ?? 0);
              const lineSubtotal = selected ? quantity * (line?.unitPrice ?? 0) : 0;
              const lineError = errors.lines?.[index]?.quantity?.message;
              return (
                <div
                  key={field.id}
                  className={`min-w-0 px-4 py-3 lg:grid lg:items-center lg:gap-3 ${LINE_HEADER_GRID_CLS} ${
                    remaining <= 0 ? 'opacity-50' : ''
                  }`}
                >
                  <div className="flex items-center">
                    <input
                      type="checkbox"
                      className="h-4 w-4 cursor-pointer accent-danger-500"
                      disabled={remaining <= 0}
                      aria-label={t('invoices.creditNote.selectLine', {
                        sku: line?.productSku ?? '',
                      })}
                      {...register(`lines.${index}.selected`)}
                    />
                  </div>

                  <div className="min-w-0">
                    <div className="truncate text-sm font-medium text-slate-900 dark:text-slate-100">
                      {line?.productName}
                    </div>
                    <div className="font-mono text-[10px] text-slate-500">{line?.productSku}</div>
                  </div>

                  <div className="mt-2 text-right text-sm tabular-nums text-slate-600 lg:mt-0 dark:text-slate-400">
                    {formatNumber(line?.quantity ?? 0, locale)}
                  </div>

                  <div className="text-right text-sm tabular-nums text-slate-600 dark:text-slate-400">
                    {formatNumber(remaining, locale)}
                  </div>

                  <div className="mt-2 lg:mt-0">
                    <input
                      className={`${fieldCls} text-right`}
                      type="number"
                      step="0.0001"
                      min="0"
                      max={remaining}
                      disabled={!selected || remaining <= 0}
                      aria-label={t('invoices.creditNote.quantity')}
                      {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
                    />
                  </div>

                  <div className="mt-2 text-right text-sm tabular-nums text-slate-600 lg:mt-0 dark:text-slate-400">
                    {formatCurrency(line?.unitPrice ?? 0, locale, invoice.currency)}
                  </div>

                  <div className="mt-2 text-right text-sm font-medium tabular-nums text-slate-900 lg:mt-0 dark:text-slate-200">
                    {formatCurrency(lineSubtotal, locale, invoice.currency)}
                  </div>

                  {lineError && (
                    <div className="mt-1 lg:col-span-7">
                      <span className="block text-[10px] text-danger-500">
                        {translateError(lineError)}
                      </span>
                    </div>
                  )}
                </div>
              );
            })}
          </DocumentLineTable>
        )}
      </div>
    </DocumentFormLayout>
  );
};
