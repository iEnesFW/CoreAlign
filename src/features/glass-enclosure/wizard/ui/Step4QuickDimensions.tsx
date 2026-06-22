import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LayoutTemplate, Ruler, SkipForward } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useUxMode } from '@/shared/lib/persona';
import { useWizardStore, type QuickRunDimensions } from '../model/wizardStore';

const fieldCls =
  'w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';
const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';

const DEFAULT_RUN: QuickRunDimensions = { widthMm: 0, heightMm: 0, panelCount: 3 };
const PRO_RUN_OPTIONS = [1, 2, 3, 4, 5] as const;
const MIN_DIM_MM = 100;
const MAX_DIM_MM = 30000;

interface QuickLayoutPreset {
  key: string;
  labelKey: string;
  defaultLabel: string;
  runs: QuickRunDimensions[];
}

const QUICK_LAYOUT_PRESETS: ReadonlyArray<QuickLayoutPreset> = [
  {
    key: 'Straight',
    labelKey: 'GlassEnclosure.NewProjectWizard.Quick.Straight',
    defaultLabel: 'Düz balkon',
    runs: [{ widthMm: 3000, heightMm: 2400, panelCount: 3, turnDeg: 0 }],
  },
  {
    key: 'LShape',
    labelKey: 'GlassEnclosure.NewProjectWizard.Quick.LShape',
    defaultLabel: 'L balkon',
    runs: [
      { widthMm: 3000, heightMm: 2400, panelCount: 3, turnDeg: 0 },
      { widthMm: 2000, heightMm: 2400, panelCount: 2, turnDeg: 90 },
    ],
  },
  {
    key: 'UShape',
    labelKey: 'GlassEnclosure.NewProjectWizard.Quick.UShape',
    defaultLabel: 'U balkon',
    runs: [
      { widthMm: 2000, heightMm: 2400, panelCount: 2, turnDeg: 0 },
      { widthMm: 3000, heightMm: 2400, panelCount: 3, turnDeg: 90 },
      { widthMm: 2000, heightMm: 2400, panelCount: 2, turnDeg: 90 },
    ],
  },
];

interface Step4QuickDimensionsProps {
  onSubmit: (skipDimensions: boolean) => void;
  isSubmitting?: boolean;
}

const parseIntOrZero = (raw: string): number => {
  if (raw === '' || raw === '-') return 0;
  const n = Number.parseInt(raw, 10);
  return Number.isFinite(n) ? n : 0;
};

export const Step4QuickDimensions = ({ onSubmit, isSubmitting }: Step4QuickDimensionsProps) => {
  const { t } = useTranslation();
  const mode = useUxMode();
  const quickDims = useWizardStore((s) => s.quickDims);
  const setQuickDims = useWizardStore((s) => s.setQuickDims);

  const [runs, setRuns] = useState<QuickRunDimensions[]>(() =>
    quickDims.runs.length > 0 ? quickDims.runs : [{ ...DEFAULT_RUN }],
  );

  useEffect(() => {
    setQuickDims({ runs, skipped: false });
  }, [runs, setQuickDims]);

  const dimsValid = useMemo(() => {
    if (runs.length === 0) return false;
    return runs.every(
      (r) =>
        r.widthMm >= MIN_DIM_MM &&
        r.widthMm <= MAX_DIM_MM &&
        r.heightMm >= MIN_DIM_MM &&
        r.heightMm <= MAX_DIM_MM &&
        r.panelCount >= 1,
    );
  }, [runs]);

  const updateRun = (index: number, patch: Partial<QuickRunDimensions>) => {
    setRuns((prev) => prev.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  };

  const setRunCount = (count: number) => {
    setRuns((prev) => {
      if (count <= prev.length) return prev.slice(0, count);
      const additions = Array.from(
        { length: count - prev.length },
        () => ({ ...DEFAULT_RUN }) as QuickRunDimensions,
      );
      return [...prev, ...additions];
    });
  };

  const handleSubmitDims = () => {
    if (!dimsValid) return;
    setQuickDims({ runs, skipped: false });
    onSubmit(false);
  };

  const handleSkip = () => {
    setQuickDims({ runs: [], skipped: true });
    onSubmit(true);
  };

  const applyQuickLayout = (preset: QuickLayoutPreset) => {
    setRuns(preset.runs.map((run) => ({ ...run })));
  };

  const isSimple = mode === 'Simple';
  const showRunSelector = !isSimple;
  const visibleRuns = isSimple ? runs.slice(0, 1) : runs;

  return (
    <section className="space-y-5">
      <header className="space-y-1">
        <h3 className="text-base font-semibold text-slate-900 dark:text-slate-100">
          {isSimple
            ? t('GlassEnclosure.NewProjectWizard.Step4.TitleSimple', {
                defaultValue: 'Hızlı ölçü gir, sonra ince ayar yaparız',
              })
            : t('GlassEnclosure.NewProjectWizard.Step4.TitlePro', {
                defaultValue: 'Run boyutları ve panel sayısı',
              })}
        </h3>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {isSimple
            ? t('GlassEnclosure.NewProjectWizard.Step4.HintSimple', {
                defaultValue: 'Sahada lazermetre ile alıp buraya yazabilirsin. Ya da sonra ölç.',
              })
            : t('GlassEnclosure.NewProjectWizard.Step4.HintPro', {
                defaultValue: 'Tahmini değerler — kesin ölçüler field-survey aşamasında alınacak.',
              })}
        </p>
      </header>

      <div>
        <span className={labelCls}>
          {t('GlassEnclosure.NewProjectWizard.Quick.Title', {
            defaultValue: 'Hazır şablonlar',
          })}
        </span>
        <div className="flex flex-wrap gap-2">
          {QUICK_LAYOUT_PRESETS.map((preset) => (
            <button
              key={preset.key}
              type="button"
              onClick={() => applyQuickLayout(preset)}
              disabled={isSubmitting}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-md border border-primary-200 bg-primary-50 px-3 py-1.5 text-xs font-medium text-primary-700',
                'transition-colors hover:bg-primary-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500',
                'disabled:cursor-not-allowed disabled:opacity-50',
                'dark:border-primary-800 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-500/20',
              )}
            >
              <LayoutTemplate size={13} />
              {t(preset.labelKey as never, { defaultValue: preset.defaultLabel })}
            </button>
          ))}
        </div>
      </div>

      {showRunSelector && (
        <div>
          <label className={labelCls} htmlFor="wizard-run-count">
            {t('GlassEnclosure.NewProjectWizard.Step4.RunCount', {
              defaultValue: 'Run sayısı',
            })}
          </label>
          <select
            id="wizard-run-count"
            value={runs.length}
            onChange={(e) => setRunCount(Number.parseInt(e.target.value, 10))}
            className={cn(fieldCls, 'max-w-[160px]')}
          >
            {PRO_RUN_OPTIONS.map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="space-y-3">
        {visibleRuns.map((run, idx) => (
          <div
            key={idx}
            className="rounded-xl border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900"
          >
            <div className="mb-2 flex items-center gap-2">
              <span className="flex h-7 w-7 items-center justify-center rounded-full bg-gradient-to-br from-primary-500 to-purple-600 text-xs font-semibold text-white">
                {idx + 1}
              </span>
              <h4 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                {t('GlassEnclosure.NewProjectWizard.Step4.RunLabel', {
                  index: idx + 1,
                  defaultValue: 'Run {{index}}',
                })}
              </h4>
            </div>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              <div>
                <label className={labelCls} htmlFor={`run-${idx}-w`}>
                  {t('GlassEnclosure.NewProjectWizard.Step4.WidthMm', {
                    defaultValue: 'Genişlik (mm)',
                  })}
                </label>
                <input
                  id={`run-${idx}-w`}
                  type="number"
                  inputMode="numeric"
                  min={MIN_DIM_MM}
                  max={MAX_DIM_MM}
                  value={run.widthMm === 0 ? '' : run.widthMm}
                  onChange={(e) => updateRun(idx, { widthMm: parseIntOrZero(e.target.value) })}
                  placeholder="3000"
                  className={fieldCls}
                />
              </div>
              <div>
                <label className={labelCls} htmlFor={`run-${idx}-h`}>
                  {t('GlassEnclosure.NewProjectWizard.Step4.HeightMm', {
                    defaultValue: 'Yükseklik (mm)',
                  })}
                </label>
                <input
                  id={`run-${idx}-h`}
                  type="number"
                  inputMode="numeric"
                  min={MIN_DIM_MM}
                  max={MAX_DIM_MM}
                  value={run.heightMm === 0 ? '' : run.heightMm}
                  onChange={(e) => updateRun(idx, { heightMm: parseIntOrZero(e.target.value) })}
                  placeholder="2200"
                  className={fieldCls}
                />
              </div>
              {!isSimple && (
                <div>
                  <label className={labelCls} htmlFor={`run-${idx}-p`}>
                    {t('GlassEnclosure.NewProjectWizard.Step4.PanelCount', {
                      defaultValue: 'Panel sayısı',
                    })}
                  </label>
                  <input
                    id={`run-${idx}-p`}
                    type="number"
                    inputMode="numeric"
                    min={1}
                    max={12}
                    value={run.panelCount === 0 ? '' : run.panelCount}
                    onChange={(e) => updateRun(idx, { panelCount: parseIntOrZero(e.target.value) })}
                    className={fieldCls}
                  />
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      <div className="rounded-md border border-dashed border-slate-300 bg-slate-50/60 px-3 py-2 text-[11px] text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
        <Ruler size={12} className="mr-1 inline" />
        {t('GlassEnclosure.NewProjectWizard.Step4.LaserHint', {
          defaultValue: 'Lazermetre entegrasyonu yakında.',
        })}
      </div>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-end">
        <button
          type="button"
          onClick={handleSkip}
          disabled={isSubmitting}
          className={cn(
            'inline-flex items-center justify-center gap-1.5 rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50',
            'disabled:cursor-not-allowed disabled:opacity-50',
            'dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800',
          )}
        >
          <SkipForward size={14} />
          {isSimple
            ? t('GlassEnclosure.NewProjectWizard.Step4.SkipSimple', {
                defaultValue: 'Atla ve sonra ölç',
              })
            : t('GlassEnclosure.NewProjectWizard.Step4.SkipPro', {
                defaultValue: 'Boyutsuz oluştur',
              })}
        </button>
        <button
          type="button"
          onClick={handleSubmitDims}
          disabled={!dimsValid || isSubmitting}
          className={cn(
            'inline-flex items-center justify-center gap-1.5 rounded-md bg-gradient-to-r from-primary-600 to-purple-600 px-4 py-2 text-sm font-semibold text-white shadow-md shadow-primary-500/20',
            'transition-opacity hover:opacity-95',
            'disabled:cursor-not-allowed disabled:opacity-50',
          )}
        >
          {isSubmitting && (
            <span
              aria-hidden
              className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/40 border-t-white"
            />
          )}
          {t('GlassEnclosure.NewProjectWizard.Step4.Create', {
            defaultValue: 'Projeyi oluştur',
          })}
        </button>
      </div>
    </section>
  );
};

export default Step4QuickDimensions;
