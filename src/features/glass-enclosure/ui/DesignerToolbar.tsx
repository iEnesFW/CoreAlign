import { useTranslation } from 'react-i18next';
import {
  Eye,
  EyeOff,
  Mountain,
  Plus,
  Redo2,
  RotateCcw,
  RotateCw,
  Save,
  Undo2,
  Wand2,
} from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import type { QualityPreset } from '../model/designerStore';
import {
  useViewerAppearance,
  VIEWER_APPEARANCE_ORDER,
  VIEWER_APPEARANCE_PRESETS,
  type ViewerAppearancePreset,
} from '../model/viewerAppearance';

interface DesignerToolbarProps {
  onAddRun: () => void;
  onSave: () => void;
  onValidate: () => void;
  onUndo: () => void;
  onRedo: () => void;
  isSaving: boolean;
  isValidating: boolean;
}

const QUALITY_LEVELS: QualityPreset[] = ['low', 'medium', 'high', 'ultra'];

export function DesignerToolbar({
  onAddRun,
  onSave,
  onValidate,
  onUndo,
  onRedo,
  isSaving,
  isValidating,
}: DesignerToolbarProps) {
  const { t } = useTranslation();
  const quality = useDesignerStore((s) => s.quality);
  const setQuality = useDesignerStore((s) => s.setQuality);
  const showAnnotations = useDesignerStore((s) => s.showAnnotations);
  const toggleAnnotations = useDesignerStore((s) => s.toggleAnnotations);
  const presentation = useDesignerStore((s) => s.presentationMode);
  const togglePresentation = useDesignerStore((s) => s.togglePresentation);
  const canUndo = useDesignerStore((s) => s.canUndo());
  const canRedo = useDesignerStore((s) => s.canRedo());
  const isDirty = useDesignerStore((s) => s.isDirty);
  const { preset: appearancePreset, setPreset: setAppearancePreset } = useViewerAppearance();

  return (
    <div className="flex flex-wrap items-center gap-2 border-b border-slate-200 bg-white px-3 py-2 dark:border-slate-700 dark:bg-slate-900">
      <button
        type="button"
        onClick={onAddRun}
        className={btnPrimary}
        aria-label={t('GlassEnclosure.Designer.NewRun')}
      >
        <Plus size={16} /> {t('GlassEnclosure.Designer.NewRun')}
      </button>
      <span className="mx-1 h-6 w-px bg-slate-300 dark:bg-slate-700" />
      <button
        type="button"
        onClick={onUndo}
        disabled={!canUndo}
        className={btnSecondary}
        aria-label={t('Common.Undo')}
      >
        <Undo2 size={16} /> {t('Common.Undo')}
      </button>
      <button
        type="button"
        onClick={onRedo}
        disabled={!canRedo}
        className={btnSecondary}
        aria-label={t('Common.Redo')}
      >
        <Redo2 size={16} /> {t('Common.Redo')}
      </button>
      <span className="mx-1 h-6 w-px bg-slate-300 dark:bg-slate-700" />
      <button
        type="button"
        onClick={toggleAnnotations}
        className={`${btnSecondary} ${showAnnotations ? 'text-blue-600' : ''}`}
        aria-label={t('GlassEnclosure.Designer.Annotations')}
        aria-pressed={showAnnotations}
      >
        {showAnnotations ? <Eye size={16} /> : <EyeOff size={16} />}{' '}
        {t('GlassEnclosure.Designer.Annotations')}
      </button>
      <button
        type="button"
        onClick={togglePresentation}
        className={`${btnSecondary} ${presentation ? 'text-purple-600' : ''}`}
        aria-pressed={presentation}
      >
        <Wand2 size={16} /> {t('GlassEnclosure.Designer.Presentation')}
      </button>
      <div className="flex items-center gap-1 rounded border border-slate-300 px-1.5 py-1 dark:border-slate-700">
        <RotateCcw size={14} className="text-slate-500" />
        <label className="sr-only" htmlFor="quality-select">
          {t('GlassEnclosure.Designer.Quality')}
        </label>
        <select
          id="quality-select"
          value={quality}
          onChange={(e) => setQuality(e.target.value as QualityPreset)}
          className="bg-transparent text-xs uppercase tracking-wide focus:outline-none"
        >
          {QUALITY_LEVELS.map((level) => (
            <option key={level} value={level}>
              {t(`GlassEnclosure.Quality.${level}` as never)}
            </option>
          ))}
        </select>
      </div>
      <div className="flex items-center gap-1 rounded border border-slate-300 px-1.5 py-1 dark:border-slate-700">
        <Mountain size={14} className="text-slate-500" />
        <label className="sr-only" htmlFor="appearance-select">
          {t('GlassEnclosure.Designer.Appearance.Label', { defaultValue: 'Görünüm' })}
        </label>
        <select
          id="appearance-select"
          value={appearancePreset}
          onChange={(e) => setAppearancePreset(e.target.value as ViewerAppearancePreset)}
          className="bg-transparent text-xs uppercase tracking-wide focus:outline-none"
        >
          {VIEWER_APPEARANCE_ORDER.map((key) => (
            <option key={key} value={key}>
              {t(VIEWER_APPEARANCE_PRESETS[key].labelKey, {
                defaultValue: VIEWER_APPEARANCE_PRESETS[key].defaultLabel,
              })}
            </option>
          ))}
        </select>
      </div>
      <span className="mx-1 h-6 w-px bg-slate-300 dark:bg-slate-700" />
      <button type="button" onClick={onValidate} disabled={isValidating} className={btnSecondary}>
        <RotateCw size={16} /> {t('GlassEnclosure.Designer.Validate')}
      </button>
      <button type="button" onClick={onSave} disabled={isSaving} className={btnPrimary}>
        <Save size={16} /> {t('GlassEnclosure.Designer.Save')}
        {isDirty && (
          <span className="ml-1 inline-block h-1.5 w-1.5 rounded-full bg-amber-400" aria-hidden />
        )}
      </button>
    </div>
  );
}

const btnPrimary =
  'inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50 focus-visible:ring-2 focus-visible:ring-blue-500';
const btnSecondary =
  'inline-flex items-center gap-1.5 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 focus-visible:ring-2 focus-visible:ring-blue-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800';
