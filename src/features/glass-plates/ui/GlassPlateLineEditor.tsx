import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import {
  plateAreaM2,
  type GlassPlateLineFormValues,
  type ReceiveGlassPlatesFormValues,
} from '../model/receiveGlassPlatesSchema';

const cellCls =
  'min-w-0 w-full rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-[#2a3143] dark:bg-[#0f111a] dark:text-slate-200';

interface Props {
  index: number;
  register: UseFormRegister<ReceiveGlassPlatesFormValues>;
  errors?: FieldErrors<GlassPlateLineFormValues>;
  plate?: GlassPlateLineFormValues;
  canRemove: boolean;
  onRemove: (index: number) => void;
}

export const GlassPlateLineEditor = ({
  index,
  register,
  errors,
  plate,
  canRemove,
  onRemove,
}: Props) => {
  const { t } = useTranslation();
  const areaM2 = plateAreaM2(plate?.widthMm, plate?.heightMm);

  const firstError =
    errors?.plateNumber?.message ??
    errors?.widthMm?.message ??
    errors?.heightMm?.message ??
    errors?.thicknessMm?.message;

  return (
    <div className="min-w-0 px-4 py-3 lg:grid lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)_3.75rem_minmax(5rem,0.8fr)] lg:items-center lg:gap-3">
      <input
        className={cellCls}
        maxLength={60}
        placeholder={t('GlassPlates.receiveForm.plateNumber')}
        aria-label={t('GlassPlates.receiveForm.plateNumber')}
        {...register(`plates.${index}.plateNumber`)}
      />

      <div className="mt-2 grid min-w-0 grid-cols-3 gap-2 lg:mt-0">
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          aria-label={t('GlassPlates.receiveForm.width')}
          {...register(`plates.${index}.widthMm`, { valueAsNumber: true })}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          aria-label={t('GlassPlates.receiveForm.height')}
          {...register(`plates.${index}.heightMm`, { valueAsNumber: true })}
        />
        <input
          className={`${cellCls} text-right`}
          type="number"
          step="any"
          min="0"
          aria-label={t('GlassPlates.receiveForm.thickness')}
          {...register(`plates.${index}.thicknessMm`, { valueAsNumber: true })}
        />
      </div>

      <div className="mt-2 flex items-center justify-end lg:mt-0">
        <button
          type="button"
          disabled={!canRemove}
          onClick={() => onRemove(index)}
          aria-label={t('GlassPlates.actions.remove')}
          className="rounded-md p-1.5 text-danger-600 transition-colors hover:bg-danger-50 disabled:opacity-40 dark:text-danger-300 dark:hover:bg-danger-900/40"
        >
          <Trash2 size={14} />
        </button>
      </div>

      <div className="mt-2 text-right text-sm font-medium tabular-nums text-slate-900 lg:mt-0 dark:text-slate-200">
        {areaM2 > 0 ? `${areaM2} m²` : '—'}
      </div>

      {firstError && (
        <div className="mt-1 lg:col-span-4">
          <span className="block text-[10px] text-danger-500">
            {t(firstError, { defaultValue: firstError })}
          </span>
        </div>
      )}
    </div>
  );
};
