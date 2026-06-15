import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useColorOptionsQuery } from '../hooks/useGlassEnclosureQueries';
import { wallFeatureModeLabelKey, wallFeatureShapeLabelKey } from '../model/wallFeatureLabels';
import { NumberField } from './WallInspector';
import type { SceneWallFeature, WallFeatureMode } from '../model/project.types';

const MODES: WallFeatureMode[] = ['recess', 'protrude', 'hole'];

interface SurfaceFeatureEditorProps {
  feature: SceneWallFeature;
  hostThicknessMm: number;
  title: string;
  onUpdate: (patch: Partial<SceneWallFeature>) => void;
  onRemove: () => void;
}

export function SurfaceFeatureEditor({
  feature,
  hostThicknessMm,
  title,
  onUpdate,
  onRemove,
}: SurfaceFeatureEditorProps) {
  const { t } = useTranslation();
  const colorsQuery = useColorOptionsQuery();
  const colors = colorsQuery.data?.data ?? [];
  const shapeLabel = wallFeatureShapeLabelKey(feature.shape);

  const commitSize = (axis: 'widthMm' | 'heightMm', value: number) => {
    const safe = Math.max(60, Math.round(value));
    const sizePatch: Partial<SceneWallFeature> =
      axis === 'widthMm' ? { widthMm: safe } : { heightMm: safe };
    if (feature.shape !== 'free' || !feature.points?.length) {
      onUpdate(sizePatch);
      return;
    }
    const previous = feature[axis];
    if (previous <= 0) return;
    const scale = safe / previous;
    sizePatch.points = feature.points.map((p) =>
      axis === 'widthMm'
        ? { x: Math.round(p.x * scale), z: p.z }
        : { x: p.x, z: Math.round(p.z * scale) },
    );
    onUpdate(sizePatch);
  };

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {title} · {t(shapeLabel.key, { defaultValue: shapeLabel.fallback })}
        </h3>
        <button
          type="button"
          onClick={onRemove}
          className="inline-flex items-center gap-1 rounded border border-red-500/40 px-2 py-1 text-xs text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
        >
          <Trash2 size={12} />
          {t('Common.Delete', { defaultValue: 'Sil' })}
        </button>
      </header>

      <div className="space-y-1.5">
        <p className="text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.WallFeature.Mode', { defaultValue: 'Davranış' })}
        </p>
        <div className="grid grid-cols-3 gap-1.5">
          {MODES.map((mode) => {
            const modeLabel = wallFeatureModeLabelKey(mode);
            return (
              <button
                key={mode}
                type="button"
                aria-pressed={feature.mode === mode}
                onClick={() =>
                  onUpdate(
                    mode === 'hole'
                      ? { mode, depthMm: hostThicknessMm }
                      : {
                          mode,
                          depthMm: Math.min(
                            Math.max(10, feature.depthMm),
                            mode === 'recess' ? Math.max(10, hostThicknessMm - 10) : 2000,
                          ),
                        },
                  )
                }
                className={cn(
                  'rounded border px-1.5 py-1.5 text-[11px] font-medium transition',
                  feature.mode === mode
                    ? 'border-blue-600 bg-blue-600 text-white'
                    : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800',
                )}
              >
                {t(modeLabel.key, { defaultValue: modeLabel.fallback })}
              </button>
            );
          })}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2">
        {feature.mode !== 'hole' && (
          <NumberField
            label={`${t('GlassEnclosure.Designer.WallFeature.Depth', { defaultValue: 'Derinlik' })} (mm)`}
            value={feature.depthMm}
            min={0}
            onCommit={(v) =>
              onUpdate({
                depthMm:
                  feature.mode === 'recess'
                    ? Math.min(Math.max(0, Math.round(v)), hostThicknessMm)
                    : Math.max(1, Math.round(v)),
              })
            }
            onDraft={() => {}}
          />
        )}
        <NumberField
          label={`${t('GlassEnclosure.Field.Width', { defaultValue: 'Genişlik' })} (mm)`}
          value={feature.widthMm}
          min={60}
          onCommit={(v) => commitSize('widthMm', v)}
          onDraft={() => {}}
        />
        <NumberField
          label={`${t('GlassEnclosure.Field.Height', { defaultValue: 'Yükseklik' })} (mm)`}
          value={feature.heightMm}
          min={60}
          onCommit={(v) => commitSize('heightMm', v)}
          onDraft={() => {}}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.WallFeature.Offset', { defaultValue: 'Konum X' })} (mm)`}
          value={feature.offsetMm}
          onCommit={(v) => onUpdate({ offsetMm: Math.round(v) })}
          onDraft={() => {}}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.WallFeature.CenterZ', { defaultValue: 'Konum Y' })} (mm)`}
          value={feature.centerZMm}
          onCommit={(v) => onUpdate({ centerZMm: Math.round(v) })}
          onDraft={() => {}}
        />
        {feature.shape === 'polygon' && (
          <NumberField
            label={t('GlassEnclosure.Designer.WallFeature.Sides', { defaultValue: 'Kenar sayısı' })}
            value={feature.sides ?? 6}
            min={3}
            onCommit={(v) => onUpdate({ sides: Math.max(3, Math.min(12, Math.round(v))) })}
            onDraft={() => {}}
          />
        )}
      </div>

      <button
        type="button"
        onClick={() => onUpdate({ side: feature.side === 1 ? -1 : 1 })}
        className="rounded border border-slate-300 px-2 py-1.5 text-xs text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800"
      >
        {t('GlassEnclosure.Designer.WallFeature.FlipSide', {
          defaultValue: 'Yüz değiştir (iç/dış)',
        })}
      </button>

      <div className="space-y-1.5">
        <p className="text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.WallFeature.Color', { defaultValue: 'Katman rengi' })}
        </p>
        <div className="flex flex-wrap items-center gap-1">
          {colors.map((color) => (
            <button
              key={color.id}
              type="button"
              title={color.name}
              aria-label={color.name}
              onClick={() => onUpdate({ colorHex: color.hexColor })}
              className={cn(
                'h-6 w-6 rounded border',
                feature.colorHex === color.hexColor
                  ? 'border-blue-500 ring-2 ring-blue-400/60'
                  : 'border-slate-300 dark:border-slate-600',
              )}
              style={{ backgroundColor: color.hexColor }}
            />
          ))}
          <button
            type="button"
            onClick={() => onUpdate({ colorHex: null })}
            className="rounded border border-slate-300 px-2 py-0.5 text-[11px] text-slate-500 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-800"
          >
            {t('GlassEnclosure.Designer.WallFeature.ColorReset', { defaultValue: 'Yüzey rengi' })}
          </button>
        </div>
      </div>

      <p className="text-[11px] text-slate-400">
        {t('GlassEnclosure.Designer.WallFeature.Hint', {
          defaultValue:
            'Genişlet aracıyla katman yüzeyini sürükleyerek derinliği ayarlayabilirsiniz; tamamen içeri itilen katman boşluğa dönüşür.',
        })}
      </p>
    </section>
  );
}

export default SurfaceFeatureEditor;
