import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ObjectAppearanceSection } from './ObjectAppearanceSection';
import { Trash2 } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerStore } from '../model/designerStore';
import type { SceneSurfaceState } from '../model/project.types';

export function SurfaceInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const surfaces = useDesignerStore((s) => s.scene.surfaces ?? []);
  const updateSurface = useDesignerStore((s) => s.updateSurface);
  const removeSurface = useDesignerStore((s) => s.removeSurface);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const surface = useMemo(
    () => surfaces.find((s) => s.id === selection.surfaceId),
    [surfaces, selection.surfaceId],
  );

  const centroid = useMemo(() => {
    const pts = surface?.points ?? [];
    const n = pts.length || 1;
    return {
      cx: pts.reduce((sum, p) => sum + p.x, 0) / n,
      cy: pts.reduce((sum, p) => sum + p.y, 0) / n,
    };
  }, [surface?.points]);

  if (!surface) return null;

  const commit = (patch: Partial<SceneSurfaceState>) => updateSurface(surface.id, patch);

  const translateTo = (axis: 'x' | 'y', value: number) => {
    const delta = Math.round(value) - Math.round(axis === 'x' ? centroid.cx : centroid.cy);
    if (delta === 0) return;
    commit({
      points: surface.points.map((p) => ({
        x: axis === 'x' ? p.x + delta : p.x,
        y: axis === 'y' ? p.y + delta : p.y,
      })),
    });
  };

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
          className="inline-flex items-center gap-1 rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
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
                ? 'border-primary-600 bg-primary-600 text-white'
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
          label={`${t('GlassEnclosure.Field.OriginX', { defaultValue: 'X' })} (mm)`}
          value={Math.round(centroid.cx)}
          onCommit={(v) => translateTo('x', v)}
        />
        <NumberField
          label={`${t('GlassEnclosure.Field.OriginY', { defaultValue: 'Y' })} (mm)`}
          value={Math.round(centroid.cy)}
          onCommit={(v) => translateTo('y', v)}
        />
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

      {/* WHY the shared section: walls and slabs already offered colour AND texture here, but a
          free-drawn surface only ever got a colour picker — even though its renderer reads
          materialKey (useTiledProceduralTexture) and the paint tool could already set one. The
          control was the only missing piece. */}
      <ObjectAppearanceSection
        colorHex={surface.colorHex}
        materialKey={surface.materialKey}
        onChange={(patch) => commit(patch)}
      />

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
        className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
      />
    </label>
  );
};

export default SurfaceInspector;
