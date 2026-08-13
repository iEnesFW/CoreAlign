import { useMemo, useState } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { toastApiError } from '@/shared/lib/mutationToast';
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
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import {
  useAdjustStock,
  useIssueStock,
  useReceiveStock,
  useStockItemsQuery,
  useTransferStock,
} from '../hooks/useInventoryQueries';
import {
  stockVoucherSchema,
  type StockVoucherFormValues,
  type StockVoucherType,
} from '../model/stockVoucherSchema';
import { StockVoucherLineEditor } from './StockVoucherLineEditor';

export type { StockVoucherType } from '../model/stockVoucherSchema';

interface Props {
  type: StockVoucherType;
  onClose: () => void;
}

const LINE_HEADER_GRID_CLS = 'lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)_3.75rem]';

const emptyLine = () => ({ productId: '', quantity: 0, unitCost: 0 });

export const StockVoucherModal = ({ type, onClose }: Props) => {
  const { t } = useTranslation();
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const warehousesQuery = useWarehousesQuery(true);
  const receiveMutation = useReceiveStock();
  const issueMutation = useIssueStock();
  const adjustMutation = useAdjustStock();
  const transferMutation = useTransferStock();

  const isTransfer = type === 'transfer';
  const [submitting, setSubmitting] = useState(false);

  const products = productsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];

  const schema = useMemo(() => stockVoucherSchema(type), [type]);
  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors, isDirty },
  } = useForm<StockVoucherFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      warehouseId: '',
      toWarehouseId: '',
      reference: '',
      notes: '',
      lines: [emptyLine()],
    },
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, true);
  const backdrop = useBackdropClick(requestClose);
  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });

  const watchedLines = useWatch({ control, name: 'lines' });
  const warehouseId = useWatch({ control, name: 'warehouseId' });

  const onHandQuery = useStockItemsQuery(
    { warehouseId: warehouseId || undefined, page: 1, pageSize: 500 },
    type === 'count' && Boolean(warehouseId),
  );
  const onHandByProduct = useMemo(() => {
    const map = new Map<string, number>();
    if (type !== 'count') return map;
    for (const it of onHandQuery.data?.data?.items ?? []) {
      map.set(it.productId, (map.get(it.productId) ?? 0) + it.onHand);
    }
    return map;
  }, [type, onHandQuery.data]);

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const onSubmit = handleSubmit(async (values) => {
    setSubmitting(true);
    const reference = values.reference?.trim() || null;
    const notes = values.notes?.trim() || null;
    const noteText = [reference, notes].filter(Boolean).join(' — ') || null;

    const results = await Promise.allSettled(
      values.lines.map((l) => {
        if (type === 'receive') {
          return receiveMutation.mutateAsync({
            productId: l.productId,
            warehouseId: values.warehouseId,
            quantity: l.quantity,
            unitCost: l.unitCost ?? 0,
            reference,
            notes,
          });
        }
        if (type === 'issue') {
          return issueMutation.mutateAsync({
            productId: l.productId,
            warehouseId: values.warehouseId,
            quantity: l.quantity,
            reference,
            notes,
          });
        }
        if (type === 'transfer') {
          return transferMutation.mutateAsync({
            productId: l.productId,
            fromWarehouseId: values.warehouseId,
            toWarehouseId: values.toWarehouseId ?? '',
            quantity: l.quantity,
            reference,
          });
        }
        const delta = l.quantity - (onHandByProduct.get(l.productId) ?? 0);
        if (delta === 0) return Promise.resolve(null);
        return adjustMutation.mutateAsync({
          productId: l.productId,
          warehouseId: values.warehouseId,
          delta,
          notes: noteText ? `Sayım: ${noteText}` : 'Sayım düzeltmesi',
        });
      }),
    );
    setSubmitting(false);

    const failed = results.filter((r) => r.status === 'rejected').length;
    const ok = results.length - failed;
    if (failed === 0) {
      toast.success(t('inventory.voucher.posted', { count: ok }));
      onClose();
      return;
    }
    const firstError = results.find((r) => r.status === 'rejected') as
      | PromiseRejectedResult
      | undefined;
    if (firstError) toastApiError(firstError.reason);
    toast.warning(t('inventory.voucher.partial', { ok, failed }));
  });

  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  const footer = (
    <>
      <div className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
        {t('inventory.voucher.quantity')}: {fields.length}
      </div>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={requestClose}
          className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-500 transition-colors hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
        >
          {t('common.cancel')}
        </button>
        <Button type="submit" isLoading={submitting}>
          {t('inventory.voucher.post')}
        </Button>
      </div>
    </>
  );

  return (
    <DocumentFormLayout
      presentation="modal"
      title={t(`inventory.voucher.title.${type}` as const)}
      closeAriaLabel={t('common.cancel')}
      onRequestClose={requestClose}
      backdropProps={backdrop}
      onSubmit={onSubmit}
      onKeyDown={onFormKeyDown}
      footer={footer}
    >
      <div className="flex flex-col gap-4">
        <div className={sectionWrapperCls}>
          <div className={sectionHeaderCls}>
            <h3 className={sectionTitleCls}>{t('inventory.voucher.warehouse')}</h3>
          </div>
          <div className={`${sectionBodyCls} grid grid-cols-1 gap-5 sm:grid-cols-2`}>
            <div>
              <label className={labelCls}>
                {isTransfer
                  ? t('inventory.voucher.fromWarehouse')
                  : t('inventory.voucher.warehouse')}
              </label>
              <select
                className={fieldCls}
                aria-label={
                  isTransfer
                    ? t('inventory.voucher.fromWarehouse')
                    : t('inventory.voucher.warehouse')
                }
                {...register('warehouseId')}
              >
                <option value="">{t('inventory.voucher.selectWarehouse')}</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code})
                  </option>
                ))}
              </select>
              {errors.warehouseId?.message && (
                <span className="mt-1 block text-[10px] text-danger-500">
                  {translateError(errors.warehouseId.message)}
                </span>
              )}
            </div>

            {isTransfer && (
              <div>
                <label className={labelCls}>{t('inventory.voucher.toWarehouse')}</label>
                <select
                  className={fieldCls}
                  aria-label={t('inventory.voucher.toWarehouse')}
                  {...register('toWarehouseId')}
                >
                  <option value="">{t('inventory.voucher.selectWarehouse')}</option>
                  {warehouses
                    .filter((w) => w.id !== warehouseId)
                    .map((w) => (
                      <option key={w.id} value={w.id}>
                        {w.name} ({w.code})
                      </option>
                    ))}
                </select>
                {errors.toWarehouseId?.message && (
                  <span className="mt-1 block text-[10px] text-danger-500">
                    {translateError(errors.toWarehouseId.message)}
                  </span>
                )}
              </div>
            )}

            <div>
              <label className={labelCls}>{t('inventory.voucher.reference')}</label>
              <input
                className={fieldCls}
                maxLength={64}
                aria-label={t('inventory.voucher.reference')}
                {...register('reference')}
              />
            </div>
            <div>
              <label className={labelCls}>{t('inventory.voucher.notes')}</label>
              <input
                className={fieldCls}
                maxLength={200}
                aria-label={t('inventory.voucher.notes')}
                {...register('notes')}
              />
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-200">
            {t('inventory.voucher.product')}
          </h2>
          <button
            type="button"
            onClick={() => append(emptyLine())}
            className="flex items-center gap-1.5 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm shadow-indigo-500/20 transition-colors hover:bg-indigo-500"
          >
            <Plus size={16} />
            {t('inventory.voucher.addLine')}
          </button>
        </div>

        <DocumentLineTable
          headerGridCls={LINE_HEADER_GRID_CLS}
          error={errors.lines?.message ? translateError(errors.lines.message) : undefined}
          header={
            <>
              <div>{t('inventory.voucher.product')}</div>
              <div className="grid min-w-0 grid-cols-2 gap-2">
                <div className="text-right">
                  {type === 'count'
                    ? t('inventory.voucher.counted')
                    : t('inventory.voucher.quantity')}
                </div>
                <div className="text-right">
                  {type === 'receive'
                    ? t('inventory.voucher.unitCost')
                    : type === 'count'
                      ? t('inventory.voucher.onHand')
                      : ''}
                </div>
              </div>
              <div aria-hidden="true" />
            </>
          }
        >
          {fields.map((field, index) => (
            <StockVoucherLineEditor
              key={field.id}
              index={index}
              type={type}
              register={register}
              errors={errors.lines?.[index]}
              line={watchedLines?.[index]}
              products={products}
              onHand={onHandByProduct.get(watchedLines?.[index]?.productId ?? '') ?? null}
              canRemove={fields.length > 1}
              onProductSelect={(i, productId) =>
                setValue(`lines.${i}.productId`, productId, {
                  shouldValidate: true,
                  shouldDirty: true,
                })
              }
              onRemove={remove}
            />
          ))}
        </DocumentLineTable>
      </div>
    </DocumentFormLayout>
  );
};
