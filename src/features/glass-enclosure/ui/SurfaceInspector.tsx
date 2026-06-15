import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerStore } from '../model/designerStore';
import { useColorOptionsQuery } from '../hooks/useGlassEnclosureQueries';
import type { SceneSurfaceState } from '../model/project.types';

export function SurfaceInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const surfaces = useDesignerStore((s) => s.scene.surfaces ?? []);
  const updateSurface = useDesignerStore((s) => s.updateSurface);
  const removeSurface = useDesignerStore((s) => s.removeSurface);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const colorsQuery = useColorOptionsQuery();
  const colors = colorsQuery.data?.data ?? [];

  const surface = useMemo(
    () => surfaces.find((s) => s.id === selection.surfaceId),
    [surfaces, selection.surfaceId],
  );

  if (!surface) return null;

  const commit = (patch: Partial<SceneSurfaceState>) => updateSurface(surface.id, patch);

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Designer.Surface.Title', { defaultValue: 'Çizilen yüzey' })}
        </h3>
        <button
          type="button"
          onClick={() => {
            removeSurface(surface.id);
            setSelection({
              kind: null,
              runId: null,
              panelId: null,
              connectionId: null,
              hardwareId: null,
              wallId: null,
              slabId: null,
              surfaceId: null,
            });
          }}
          className="inline-flex items-center gap-1 rounded border border-red-500/40 px-2 py-1 text-xs text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
        >
          <Trash2 size={12} />
          {t('Common.Delete', { defaultValue: 'Sil' })}
        </button>
      </header>

      <div className="flex gap-1.5">
        {(['floor', 'roof'] as const).map((kind) => (
          <button
            key={kind}
            type="button"
            onClick={() => commit({ kind })}
            className={cn(
              'flex-1 rounded border px-2 py-1 text-xs font-medium transition',
              surface.kind === kind
                ? 'border-blue-600 bg-blue-600 text-white'
                : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800',
            )}
          >
            {kind === 'roof'
              ? t('GlassEnclosure.Designer.Slab.Roof', { defaultValue: 'Çatı' })
              : t('GlassEnclosure.Designer.Slab.Floor', { defaultValue: 'Zemin' })}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-2 gap-2">
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Elevation', { defaultValue: 'Kot' })} (mm)`}
          value={surface.elevationMm}
          onCommit={(v) => commit({ elevationMm: Math.round(v) })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Thickness', { defaultValue: 'Kalınlık' })} (mm)`}
          value={surface.thicknessMm}
          min={10}
          onCommit={(v) => commit({ thicknessMm: Math.max(10, Math.round(v)) })}
        />
      </div>

      <div className="space-y-1.5">
        <p className="text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.WallFeature.Color', { defaultValue: 'Renk' })}
        </p>
        <div className="flex flex-wrap items-center gap-1">
          {colors.map((color) => (
            <button
              key={color.id}
              type="button"
              title={color.name}
              aria-label={color.name}
              onClick={() => commit({ colorHex: color.hexColor, materialKey: null })}
              className={cn(
                'h-6 w-6 rounded border',
                surface.colorHex === color.hexColor
                  ? 'border-blue-500 ring-2 ring-blue-400/60'
                  : 'border-slate-300 dark:border-slate-600',
              )}
              style={{ backgroundColor: color.hexColor }}
            />
          ))}
        </div>
      </div>

      <p className="text-[11px] text-slate-400">
        {t('GlassEnclosure.Designer.Surface.Hint', {
          defaultValue:
            'Taşı aracıyla yüzeyi sürükleyebilir, Boya aracıyla renk/malzeme verebilirsiniz.',
        })}
      </p>
    </section>
  );
}

const NumberField = ({
  label,
  value,
  min,
  onCommit,
}: {
  label: string;
  value: number;
  min?: number;
  onCommit: (value: number) => void;
}) => {
  const [draft, setDraft] = useState(String(value));
  const [tracked, setTracked] = useState(value);
  if (value !== tracked) {
    setTracked(value);
    setDraft(String(value));
  }
  return (
    <label className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
      <span className="text-[10px] uppercase tracking-wide">{label}</span>
      <input
        type="number"
        min={min}
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onBlur={() => {
          const parsed = Number(draft);
          if (!Number.isNaN(parsed)) onCommit(parsed);
        }}
        className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-blue-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
      />
    </label>
  );
};

export default SurfaceInspector;
