import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';

interface PitchedRoofInspectorProps {
  className?: string;
}

const MIN_PITCH_DEG = 10;
const MAX_PITCH_DEG = 45;
const MIN_HEIGHT_MM = 500;
const MAX_HEIGHT_MM = 8000;

const numberOrEmpty = (value: number | null | undefined): string =>
  typeof value === 'number' ? String(value) : '';

export function PitchedRoofInspector({ className }: PitchedRoofInspectorProps) {
  const { t } = useTranslation();
  const project = useDesignerStore((s) => s.project);
  const updatePitchedRoof = useDesignerStore((s) => s.updatePitchedRoof);

  const isApplicable = useMemo(
    () => project?.enclosureSubtype === 'Greenhouse' && project?.geometryMode === 'Pitched',
    [project?.enclosureSubtype, project?.geometryMode],
  );

  const [pitchDraft, setPitchDraft] = useState<string>(numberOrEmpty(project?.roofPitchDeg));
  const [ridgeDraft, setRidgeDraft] = useState<string>(numberOrEmpty(project?.ridgeHeightMm));
  const [eaveDraft, setEaveDraft] = useState<string>(numberOrEmpty(project?.eaveHeightMm));
  const [trackedKey, setTrackedKey] = useState<string>(`${project?.id ?? ''}`);
  const currentKey = `${project?.id ?? ''}`;
  if (currentKey !== trackedKey) {
    setTrackedKey(currentKey);
    setPitchDraft(numberOrEmpty(project?.roofPitchDeg));
    setRidgeDraft(numberOrEmpty(project?.ridgeHeightMm));
    setEaveDraft(numberOrEmpty(project?.eaveHeightMm));
  }

  if (!isApplicable || !project) return null;

  const ridgeValue = ridgeDraft ? Number(ridgeDraft) : null;
  const eaveValue = eaveDraft ? Number(eaveDraft) : null;
  const pitchValue = pitchDraft ? Number(pitchDraft) : null;

  const pitchOutOfRange =
    pitchValue !== null && (pitchValue < MIN_PITCH_DEG || pitchValue > MAX_PITCH_DEG);
  const ridgeNotGreaterThanEave =
    ridgeValue !== null && eaveValue !== null && ridgeValue <= eaveValue;

  const commitPitch = (raw: string) => {
    setPitchDraft(raw);
    if (!raw) return;
    const value = Number(raw);
    if (Number.isFinite(value)) {
      updatePitchedRoof({ roofPitchDeg: value });
    }
  };

  const commitRidge = (raw: string) => {
    setRidgeDraft(raw);
    if (!raw) return;
    const value = Math.round(Number(raw));
    if (Number.isFinite(value)) {
      updatePitchedRoof({ ridgeHeightMm: value });
    }
  };

  const commitEave = (raw: string) => {
    setEaveDraft(raw);
    if (!raw) return;
    const value = Math.round(Number(raw));
    if (Number.isFinite(value)) {
      updatePitchedRoof({ eaveHeightMm: value });
    }
  };

  return (
    <section
      className={
        className ??
        'space-y-3 rounded-md border border-success-200 bg-success-50/60 p-3 dark:border-success-900/50 dark:bg-success-950/20'
      }
      aria-label={t('GlassEnclosure.Designer.Greenhouse.PitchParam.SectionTitle')}
    >
      <h4 className="text-xs font-semibold uppercase tracking-wide text-success-700 dark:text-success-300">
        {t('GlassEnclosure.Designer.Greenhouse.PitchParam.SectionTitle')}
      </h4>

      <PitchedField
        label={t('GlassEnclosure.Designer.Greenhouse.PitchParam.RoofPitch')}
        suffix="°"
        value={pitchDraft}
        onChange={commitPitch}
        min={MIN_PITCH_DEG}
        max={MAX_PITCH_DEG}
        step={1}
        error={
          pitchOutOfRange
            ? t('GlassEnclosure.Designer.Greenhouse.PitchParam.PitchOutOfRange', {
                min: MIN_PITCH_DEG,
                max: MAX_PITCH_DEG,
              })
            : null
        }
      />
      <PitchedField
        label={t('GlassEnclosure.Designer.Greenhouse.PitchParam.RidgeHeight')}
        suffix="mm"
        value={ridgeDraft}
        onChange={commitRidge}
        min={MIN_HEIGHT_MM}
        max={MAX_HEIGHT_MM}
        step={50}
        error={
          ridgeNotGreaterThanEave
            ? t('GlassEnclosure.Designer.Greenhouse.PitchParam.RidgeMustExceedEave')
            : null
        }
      />
      <PitchedField
        label={t('GlassEnclosure.Designer.Greenhouse.PitchParam.EaveHeight')}
        suffix="mm"
        value={eaveDraft}
        onChange={commitEave}
        min={MIN_HEIGHT_MM}
        max={MAX_HEIGHT_MM}
        step={50}
        error={null}
      />
    </section>
  );
}

interface PitchedFieldProps {
  label: string;
  suffix: string;
  value: string;
  onChange: (next: string) => void;
  min: number;
  max: number;
  step: number;
  error: string | null;
}

const PitchedField = ({
  label,
  suffix,
  value,
  onChange,
  min,
  max,
  step,
  error,
}: PitchedFieldProps) => (
  <label className="block space-y-1">
    <span className="block text-[11px] font-medium uppercase tracking-wide text-slate-600 dark:text-slate-300">
      {label}
    </span>
    <div className="flex items-center gap-1">
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        step={step}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
      />
      <span className="text-[10px] text-slate-500 dark:text-slate-400">{suffix}</span>
    </div>
    {error ? (
      <span className="block text-[10px] text-danger-600 dark:text-danger-400">{error}</span>
    ) : null}
  </label>
);
