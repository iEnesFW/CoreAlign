import { useMemo, useState } from 'react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import { useBackdropClick } from '@/shared/hooks/useBackdropClick';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
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
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useReceiveGlassPlates, useStorageLocationsQuery } from '../hooks/useGlassPlateQueries';
import {
  plateAreaM2,
  receiveGlassPlatesSchema,
  type ReceiveGlassPlatesFormValues,
} from '../model/receiveGlassPlatesSchema';
import { GlassPlateLineEditor } from './GlassPlateLineEditor';

interface Props {
  onClose: () => void;
  initialProductId?: string;
  initialWarehouseId?: string;
}

const LINE_HEADER_GRID_CLS =
  'lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5rem,0.8fr)]';

const emptyPlate = (defaults?: { widthMm?: number; heightMm?: number; thicknessMm?: number }) => ({
  plateNumber: '',
  widthMm: defaults?.widthMm ?? 0,
  heightMm: defaults?.heightMm ?? 0,
  thicknessMm: defaults?.thicknessMm ?? 0,
});

export const ReceiveGlassPlatesModal = ({
  onClose,
  initialProductId,
  initialWarehouseId,
}: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const warehousesQuery = useWarehousesQuery(true);
  const receiveMutation = useReceiveGlassPlates();

  const products = productsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];
  const [submitting, setSubmitting] = useState(false);

  const {
    register,
    control,
    handleSubmit,
    setValue,
    getValues,
    formState: { errors, isDirty },
  } = useForm<ReceiveGlassPlatesFormValues>({
    resolver: zodResolver(receiveGlassPlatesSchema),
    defaultValues: {
      productId: initialProductId ?? '',
      warehouseId: initialWarehouseId ?? '',
      storageLocationId: '',
      unitCostPerM2: '',
      notes: '',
      plates: [emptyPlate()],
    },
    mode: 'onTouched',
  });

  const requestClose = useModalClose(isDirty, onClose, true);
  const backdrop = useBackdropClick(requestClose);
  const { fields, append, remove } = useFieldArray({ control, name: 'plates' });

  const productId = useWatch({ control, name: 'productId' });
  const warehouseId = useWatch({ control, name: 'warehouseId' });
  const unitCostPerM2 = useWatch({ control, name: 'unitCostPerM2' });
  const watchedPlates = useWatch({ control, name: 'plates' });

  const locationsQuery = useStorageLocationsQuery(warehouseId || undefined);
  const locations = warehouseId ? (locationsQuery.data ?? []) : [];

  const summary = useMemo(() => {
    const totalAreaM2 = (watchedPlates ?? []).reduce(
      (sum, p) => sum + plateAreaM2(p.widthMm, p.heightMm),
      0,
    );
    const unitCost = Number(unitCostPerM2) || 0;
    return {
      plateCount: watchedPlates?.length ?? 0,
      totalAreaM2: Math.round(totalAreaM2 * 10000) / 10000,
      totalValue: Math.round(totalAreaM2 * unitCost * 100) / 100,
    };
  }, [watchedPlates, unitCostPerM2]);

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const addPlate = () => {
    const last = getValues('plates').at(-1);
    append(
      emptyPlate({
        widthMm: last?.widthMm,
        heightMm: last?.heightMm,
        thicknessMm: last?.thicknessMm,
      }),
    );
  };

  const onSubmit = handleSubmit(async (values) => {
    setSubmitting(true);
    const result = await receiveMutation
      .mutateAsync({
        productId: values.productId,
        warehouseId: values.warehouseId,
        storageLocationId: values.storageLocationId || null,
        unitCostPerM2: Number(values.unitCostPerM2) || 0,
        notes: values.notes?.trim() || null,
        plates: values.plates.map((p) => ({
          plateNumber: p.plateNumber.trim(),
          widthMm: p.widthMm,
          heightMm: p.heightMm,
          thicknessMm: p.thicknessMm,
        })),
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(
        t('GlassPlates.receiveForm.received', {
          count: result.data?.plateCount ?? values.plates.length,
        }),
      );
      onClose();
      return;
    }
    if (result) {
      toast.error(result.errors?.[0] ?? t('GlassPlates.receiveForm.failed'));
    }
  });

  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  const footer = (
    <>
      <div className="text-sm">
        <span className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('GlassPlates.receiveForm.totalArea')}
        </span>{' '}
        <span className="font-semibold text-slate-900 dark:text-slate-100">
          {formatNumber(summary.totalAreaM2, locale)} m²
        </span>
        {summary.totalValue > 0 && (
          <div className="text-[10px] text-slate-400 dark:text-slate-500">
            {t('GlassPlates.receiveForm.totalValue')}:{' '}
            {formatCurrency(summary.totalValue, locale, 'TRY')}
          </div>
        )}
      </div>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={requestClose}
          className="rounded-lg px-4 py-2 text-sm font-semibold text-slate-500 transition-colors hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
        >
          {t('GlassPlates.actions.cancel')}
        </button>
        <Button type="submit" isLoading={submitting}>
          {t('GlassPlates.actions.save')}
        </Button>
      </div>
    </>
  );

  return (
    <DocumentFormLayout
      presentation="modal"
      title={t('GlassPlates.receiveForm.title')}
      closeAriaLabel={t('GlassPlates.actions.cancel')}
      onRequestClose={requestClose}
      backdropProps={backdrop}
      onSubmit={onSubmit}
      onKeyDown={onFormKeyDown}
      footer={footer}
    >
      <div className="flex flex-col gap-4">
        <div className={sectionWrapperCls}>
          <div className={sectionHeaderCls}>
            <h3 className={sectionTitleCls}>{t('GlassPlates.receiveForm.title')}</h3>
          </div>
          <div className={`${sectionBodyCls} grid grid-cols-1 gap-5 sm:grid-cols-2`}>
            <div>
              <label className={labelCls}>{t('GlassPlates.receiveForm.product')}</label>
              <ProductPicker
                products={products}
                value={productId}
                invalid={Boolean(errors.productId)}
                onSelect={(id) =>
                  setValue('productId', id, { shouldValidate: true, shouldDirty: true })
                }
              />
              {errors.productId?.message && (
                <span className="mt-1 block text-[10px] text-danger-500">
                  {translateError(errors.productId.message)}
                </span>
              )}
            </div>

            <div>
              <label className={labelCls}>{t('GlassPlates.receiveForm.warehouse')}</label>
              <select
                className={fieldCls}
                aria-label={t('GlassPlates.receiveForm.warehouse')}
                {...register('warehouseId', {
                  onChange: () => setValue('storageLocationId', ''),
                })}
              >
                <option value="">{t('GlassPlates.locationForm.selectWarehouse')}</option>
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
              <label className={labelCls}>{t('GlassPlates.receiveForm.location')}</label>
              <select
                className={fieldCls}
                aria-label={t('GlassPlates.receiveForm.location')}
                disabled={!warehouseId || locations.length === 0}
                {...register('storageLocationId')}
              >
                <option value="">{t('GlassPlates.receiveForm.noLocation')}</option>
                {locations.map((l) => (
                  <option key={l.id} value={l.id}>
                    {l.code} — {l.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className={labelCls}>{t('GlassPlates.receiveForm.unitCostPerM2')}</label>
              <input
                className={fieldCls}
                type="number"
                step="any"
                min="0"
                aria-label={t('GlassPlates.receiveForm.unitCostPerM2')}
                {...register('unitCostPerM2')}
              />
            </div>

            <div className="col-span-1 sm:col-span-2">
              <label className={labelCls}>{t('GlassPlates.receiveForm.notes')}</label>
              <input
                className={fieldCls}
                maxLength={200}
                aria-label={t('GlassPlates.receiveForm.notes')}
                {...register('notes')}
              />
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold tracking-tight text-slate-900 dark:text-slate-200">
            {t('GlassPlates.receiveForm.plateCount', { n: summary.plateCount })}
          </h2>
          <button
            type="button"
            onClick={addPlate}
            className="flex items-center gap-1.5 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm shadow-indigo-500/20 transition-colors hover:bg-indigo-500"
          >
            <Plus size={16} />
            {t('GlassPlates.receiveForm.addPlate')}
          </button>
        </div>

        <DocumentLineTable
          headerGridCls={LINE_HEADER_GRID_CLS}
          error={errors.plates?.message ? translateError(errors.plates.message) : undefined}
          header={
            <>
              <div>{t('GlassPlates.receiveForm.plateNumber')}</div>
              <div className="grid min-w-0 grid-cols-3 gap-2">
                <div className="text-right">{t('GlassPlates.receiveForm.width')}</div>
                <div className="text-right">{t('GlassPlates.receiveForm.height')}</div>
                <div className="text-right">{t('GlassPlates.receiveForm.thickness')}</div>
              </div>
              <div aria-hidden="true" />
              <div className="text-right">{t('GlassPlates.receiveForm.area')}</div>
            </>
          }
        >
          {fields.map((field, index) => (
            <GlassPlateLineEditor
              key={field.id}
              index={index}
              register={register}
              errors={errors.plates?.[index]}
              plate={watchedPlates?.[index]}
              canRemove={fields.length > 1}
              onRemove={remove}
            />
          ))}
        </DocumentLineTable>
      </div>
    </DocumentFormLayout>
  );
};
