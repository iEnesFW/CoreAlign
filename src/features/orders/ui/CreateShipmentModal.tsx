import { useEffect, useMemo, useRef } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Truck } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { formatNumber } from '@/shared/lib/format';
import { toastApiError } from '@/shared/lib/mutationToast';
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
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useCreateShipment, useShipmentsByOrderQuery } from '../hooks/useOrderQueries';
import { createShipmentSchema, type CreateShipmentFormValues } from '../model/createShipmentSchema';
import { availableToShip, claimedQuantityByOrderLine } from '../model/shipmentAvailability';
import type { Order } from '../model/order.types';

interface Props {
  order: Order;
  onClose: () => void;
}

const LINE_HEADER_GRID_CLS =
  'lg:grid-cols-[2.25rem_minmax(0,3fr)_minmax(5rem,1fr)_minmax(5rem,1fr)_minmax(6rem,1fr)]';

export const CreateShipmentModal = ({ order, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const warehousesQuery = useWarehousesQuery(true);
  const shipmentsQuery = useShipmentsByOrderQuery(order.id);
  const createMutation = useCreateShipment();

  const warehouses = useMemo(() => warehousesQuery.data?.data ?? [], [warehousesQuery.data]);
  const shipments = useMemo(() => shipmentsQuery.data?.data ?? [], [shipmentsQuery.data]);

  const defaults = useMemo<CreateShipmentFormValues>(() => {
    const claimed = claimedQuantityByOrderLine(shipments);
    return {
      warehouseId: warehouses.find((w) => w.isDefault)?.id ?? warehouses[0]?.id ?? '',
      notes: '',
      lines: order.lines.map((line) => {
        const available = availableToShip(line.quantityRemainingToShip, claimed.get(line.id) ?? 0);
        return {
          orderLineId: line.id,
          selected: available > 0,
          quantity: available,
          available,
        };
      }),
    };
  }, [order.lines, shipments, warehouses]);

  const {
    register,
    control,
    handleSubmit,
    reset,
    getValues,
    setValue,
    formState: { errors, isDirty },
  } = useForm<CreateShipmentFormValues>({
    resolver: zodResolver(createShipmentSchema),
    defaultValues: defaults,
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, true);
  const backdrop = useBackdropClick(requestClose);
  const { fields } = useFieldArray({ control, name: 'lines' });

  const watchedLines = useWatch({ control, name: 'lines' });

  // WHY the caps are re-seeded: the default warehouse and the quantity already claimed by open
  // shipments arrive after mount, so the first render always seeds an empty warehouse and an
  // un-netted cap. keepDirtyValues preserves what the user typed; the clamp then pulls any
  // now-too-high quantity down to the cap the server will enforce.
  const appliedSignature = useRef<string | null>(null);
  useEffect(() => {
    const signature = `${defaults.warehouseId}|${defaults.lines
      .map((l) => `${l.orderLineId}:${l.available}`)
      .join(',')}`;
    if (appliedSignature.current === signature) return;
    appliedSignature.current = signature;
    reset(defaults, { keepDirtyValues: true });
    getValues('lines').forEach((line, index) => {
      if (line.quantity > line.available) {
        setValue(`lines.${index}.quantity`, line.available, { shouldValidate: true });
      }
    });
  }, [defaults, reset, getValues, setValue]);

  const summary = useMemo(() => {
    const active = (watchedLines ?? []).filter((l) => l.selected && Number(l.quantity) > 0);
    return {
      lineCount: active.length,
      totalQuantity: active.reduce((sum, l) => sum + Number(l.quantity), 0),
    };
  }, [watchedLines]);

  const shippableCount = defaults.lines.filter((l) => l.available > 0).length;

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const onSubmit = handleSubmit(async (values) => {
    const lines = values.lines
      .filter((l) => l.selected && l.quantity > 0)
      .map((l) => ({ orderLineId: l.orderLineId, quantity: l.quantity, notes: null }));

    try {
      await createMutation.mutateAsync({
        orderId: order.id,
        warehouseId: values.warehouseId,
        lines,
        notes: values.notes?.trim() || null,
      });
      toast.success(t('orders.actions.createShipment'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  });

  const footer = (
    <>
      <div className="text-sm">
        <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('orders.shipments.totalQuantity')}
        </span>{' '}
        <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
          {formatNumber(summary.totalQuantity, locale)}
        </span>
        <div className="text-[10px] text-slate-400 dark:text-slate-500">
          {t('orders.shipments.selectedLines', { n: summary.lineCount })}
        </div>
      </div>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={requestClose}
          className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-500 transition-colors hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
        >
          {t('common.cancel')}
        </button>
        <Button type="submit" isLoading={createMutation.isPending}>
          <Truck size={14} />
          {t('orders.actions.createShipment')}
        </Button>
      </div>
    </>
  );

  return (
    <DocumentFormLayout
      presentation="modal"
      title={t('orders.shipments.createTitle')}
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
              {order.orderNumber} · {order.customerName}
            </h3>
          </div>
          <div className={`${sectionBodyCls} grid grid-cols-1 gap-5 sm:grid-cols-2`}>
            <div>
              <label className={labelCls}>{t('inventory.adjust.warehouse')}</label>
              <select
                className={fieldCls}
                aria-label={t('inventory.adjust.warehouse')}
                {...register('warehouseId')}
              >
                <option value="">{t('inventory.adjust.warehousePlaceholder')}</option>
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

            <div>
              <label className={labelCls}>{t('inventory.adjust.notes')}</label>
              <input
                className={fieldCls}
                maxLength={500}
                aria-label={t('inventory.adjust.notes')}
                {...register('notes')}
              />
            </div>
          </div>
        </div>

        <h2 className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-200">
          {t('orders.shipments.selectLines')}
        </h2>

        {shippableCount === 0 ? (
          <div className={`${sectionWrapperCls} px-4 py-6 text-center text-xs text-slate-500`}>
            {t('orders.shipments.nothingLeft')}
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
                <div>{t('orders.shipments.product')}</div>
                <div className="text-right">{t('orders.shipments.ordered')}</div>
                <div className="text-right">{t('orders.shipments.available')}</div>
                <div className="text-right">{t('orders.shipments.shipQuantity')}</div>
              </>
            }
          >
            {fields.map((field, index) => {
              const line = order.lines[index];
              const available = watchedLines?.[index]?.available ?? 0;
              const selected = watchedLines?.[index]?.selected ?? false;
              const lineError = errors.lines?.[index]?.quantity?.message;
              return (
                <div
                  key={field.id}
                  className={`min-w-0 px-4 py-3 lg:grid lg:items-center lg:gap-3 ${LINE_HEADER_GRID_CLS} ${
                    available <= 0 ? 'opacity-50' : ''
                  }`}
                >
                  <div className="flex items-center">
                    <input
                      type="checkbox"
                      className="h-4 w-4 cursor-pointer accent-indigo-600"
                      disabled={available <= 0}
                      aria-label={t('orders.shipments.selectLine', { sku: line?.productSku ?? '' })}
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
                    {formatNumber(available, locale)}
                  </div>

                  <div className="mt-2 lg:mt-0">
                    <input
                      className={`${fieldCls} text-right`}
                      type="number"
                      step="0.0001"
                      min="0"
                      max={available}
                      disabled={!selected || available <= 0}
                      aria-label={t('orders.shipments.shipQuantity')}
                      {...register(`lines.${index}.quantity`, { valueAsNumber: true })}
                    />
                  </div>

                  {lineError && (
                    <div className="mt-1 lg:col-span-5">
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
