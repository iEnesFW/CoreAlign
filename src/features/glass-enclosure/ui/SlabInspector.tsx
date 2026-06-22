import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { queueToast } from '@/shared/api/toastQueue';
import { useDesignerStore } from '../model/designerStore';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  penetratesAny,
} from '../scene/interaction/planCollision';
import { wallFeatureModeLabelKey, wallFeatureShapeLabelKey } from '../model/wallFeatureLabels';
import type { SceneSlabState } from '../model/project.types';

export function SlabInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  const walls = useDesignerStore((s) => s.scene.walls ?? []);
  const runs = useDesignerStore((s) => s.scene.runs);
  const updateSlab = useDesignerStore((s) => s.updateSlab);
  const removeSlab = useDesignerStore((s) => s.removeSlab);
  const removeSlabFeature = useDesignerStore((s) => s.removeSlabFeature);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const slab = useMemo(
    () => slabs.find((item) => item.id === selection.slabId),
    [slabs, selection.slabId],
  );
  const obstacles = useMemo(
    () => [
      ...walls.map((w) => buildWallFootprint(w, 0, 0, w.rotationDeg)),
      ...runs.map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg)),
      ...slabs.map((s) => buildSlabFootprint(s, 0, 0, s.rotationDeg)),
    ],
    [walls, runs, slabs],
  );
  const [draft, setDraft] = useState(slab);
  const [tracked, setTracked] = useState(slab);
  if (slab !== tracked) {
    setTracked(slab);
    setDraft(slab);
  }

  if (!slab || !draft) return null;

  // A barrel roof defers surface features (they aren't projected onto the curve, #6b).
  const isBarrelRoof = slab.kind === 'roof' && (draft.arcRiseMm ?? 0) > 0;

  const commit = (patch: Partial<typeof slab>) => {
    const candidate: SceneSlabState = { ...slab, ...patch };
    const alreadyColliding = penetratesAny(
      buildSlabFootprint(slab, 0, 0, slab.rotationDeg),
      obstacles,
    );
    const wouldCollide = penetratesAny(
      buildSlabFootprint(candidate, 0, 0, candidate.rotationDeg),
      obstacles,
    );
    if (!alreadyColliding && wouldCollide) {
      queueToast({
        dedupeKey: 'glass-collision-blocked',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.CollisionBlocked', {
          defaultValue: 'Bu değer başka bir nesneyle çakışıyor — uygulanmadı.',
        }),
      });
      setDraft(slab);
      return;
    }
    updateSlab(slab.id, patch);
  };

  // Barrel roofs can't carry surface features yet (#6b); converting to a curve drops
  // them rather than leaving data that vanishes from the 3D view but lingers.
  const commitArcRise = (v: number) => {
    const rise = v > 0 ? Math.round(v) : null;
    const dropFeatures = rise !== null && (slab.features ?? []).length > 0;
    commit({ arcRiseMm: rise, ...(dropFeatures ? { features: [] } : {}) });
    if (dropFeatures) {
      queueToast({
        dedupeKey: 'glass-arc-drops-features',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Slab.BarrelDropsFeatures', {
          defaultValue: 'Kavise çevirince bu çatının şekilleri kaldırıldı.',
        }),
      });
    }
  };

  const handleDelete = () => {
    removeSlab(slab.id);
    setSelection({
      kind: null,
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: null,
    });
  };

  const kindLabel = (kind: SceneSlabState['kind']) =>
    kind === 'roof'
      ? t('GlassEnclosure.Designer.Slab.Roof', { defaultValue: 'Çatı' })
      : t('GlassEnclosure.Designer.Slab.Floor', { defaultValue: 'Zemin' });

  const deleteLabel =
    slab.kind === 'roof'
      ? t('GlassEnclosure.Designer.Slab.DeleteRoof', { defaultValue: 'Çatıyı sil' })
      : t('GlassEnclosure.Designer.Slab.DeleteFloor', { defaultValue: 'Zemini sil' });

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {kindLabel(slab.kind)}
        </h3>
        <button
          type="button"
          onClick={handleDelete}
          className="rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
        >
          {deleteLabel}
        </button>
      </header>

      <div className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
        <span className="text-[10px] uppercase tracking-wide">
          {t('GlassEnclosure.Designer.Slab.Kind', { defaultValue: 'Tip' })}
        </span>
        <div className="flex gap-1.5">
          {(['floor', 'roof'] as const).map((kind) => (
            <button
              key={kind}
              type="button"
              onClick={() => commit({ kind })}
              className={
                slab.kind === kind
                  ? 'flex-1 rounded border border-primary-500 bg-primary-50 px-2 py-1 text-xs font-medium text-primary-600 dark:bg-primary-950/30 dark:text-primary-400'
                  : 'flex-1 rounded border border-slate-300 px-2 py-1 text-xs text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800'
              }
            >
              {kindLabel(kind)}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <NumberField
          label={`${t('GlassEnclosure.Field.Length', { defaultValue: 'Uzunluk' })} (mm)`}
          value={draft.lengthMm}
          min={100}
          onCommit={(v) => commit({ lengthMm: v })}
          onDraft={(v) => setDraft({ ...draft, lengthMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Depth', { defaultValue: 'Derinlik' })} (mm)`}
          value={draft.depthMm}
          min={100}
          onCommit={(v) => commit({ depthMm: v })}
          onDraft={(v) => setDraft({ ...draft, depthMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Thickness', { defaultValue: 'Kalınlık' })} (mm)`}
          value={draft.thicknessMm}
          min={20}
          onCommit={(v) => commit({ thicknessMm: v })}
          onDraft={(v) => setDraft({ ...draft, thicknessMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Elevation', { defaultValue: 'Kot' })} (mm)`}
          value={draft.elevationMm}
          onCommit={(v) => commit({ elevationMm: v })}
          onDraft={(v) => setDraft({ ...draft, elevationMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Field.OriginX', { defaultValue: 'X' })} (mm)`}
          value={draft.originX}
          onCommit={(v) => commit({ originX: v })}
          onDraft={(v) => setDraft({ ...draft, originX: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Field.OriginY', { defaultValue: 'Y' })} (mm)`}
          value={draft.originY}
          onCommit={(v) => commit({ originY: v })}
          onDraft={(v) => setDraft({ ...draft, originY: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Rotation', { defaultValue: 'Dönüş' })} (°)`}
          value={draft.rotationDeg}
          onCommit={(v) => commit({ rotationDeg: v })}
          onDraft={(v) => setDraft({ ...draft, rotationDeg: v })}
        />
      </div>

      {slab.kind === 'roof' && (
        <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Designer.Slab.BarrelTitle', {
              defaultValue: 'Beşik (kavisli) çatı',
            })}
          </p>
          <NumberField
            label={`${t('GlassEnclosure.Designer.Slab.ArcRise', { defaultValue: 'Kavis yüksekliği' })} (mm)`}
            value={draft.arcRiseMm ?? 0}
            min={0}
            onCommit={(v) => commitArcRise(v)}
            onDraft={(v) => setDraft({ ...draft, arcRiseMm: v })}
          />
        </div>
      )}

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Corner.Title', { defaultValue: 'Köşe ovalliği (mm)' })}
        </p>
        <div className="grid grid-cols-4 gap-1.5">
          {(
            [
              ['tl', t('GlassEnclosure.Designer.Corner.TL', { defaultValue: 'Sol üst' })],
              ['tr', t('GlassEnclosure.Designer.Corner.TR', { defaultValue: 'Sağ üst' })],
              ['bl', t('GlassEnclosure.Designer.Corner.BL', { defaultValue: 'Sol alt' })],
              ['br', t('GlassEnclosure.Designer.Corner.BR', { defaultValue: 'Sağ alt' })],
            ] as const
          ).map(([key, cornerLabel]) => (
            <NumberField
              key={key}
              label={cornerLabel}
              value={slab.cornerRadiiMm?.[key] ?? 0}
              min={0}
              onCommit={(v) =>
                commit({
                  cornerRadiiMm: {
                    ...slab.cornerRadiiMm,
                    [key]: Math.max(0, Math.round(v)),
                  },
                })
              }
              onDraft={() => {}}
            />
          ))}
        </div>
      </div>

      {!isBarrelRoof && (
        <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Designer.WallFeature.ListTitle', { defaultValue: 'Katmanlar' })}
          </p>
          {(slab.features ?? []).length === 0 ? (
            <p className="text-[11px] text-slate-400">
              {t('GlassEnclosure.Designer.WallFeature.None', {
                defaultValue: "Katman yok — üstteki 'Yüzeye çiz' aracıyla yüzeye şekil çizin.",
              })}
            </p>
          ) : (
            (slab.features ?? []).map((feature) => {
              const shapeLabel = wallFeatureShapeLabelKey(feature.shape);
              const modeLabel = wallFeatureModeLabelKey(feature.mode);
              return (
                <div
                  key={feature.id}
                  className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 dark:border-slate-700"
                >
                  <button
                    type="button"
                    onClick={() =>
                      setSelection({
                        kind: 'slabFeature',
                        runId: null,
                        panelId: null,
                        connectionId: null,
                        hardwareId: null,
                        wallId: null,
                        slabId: slab.id,
                        featureId: feature.id,
                      })
                    }
                    className="min-w-0 flex-1 truncate text-left text-[11px] font-medium text-slate-600 hover:text-primary-600 dark:text-slate-300 dark:hover:text-primary-400"
                  >
                    {t(shapeLabel.key, { defaultValue: shapeLabel.fallback })} ·{' '}
                    {t(modeLabel.key, { defaultValue: modeLabel.fallback })} · {feature.widthMm}×
                    {feature.heightMm}
                  </button>
                  <button
                    type="button"
                    onClick={() => removeSlabFeature(slab.id, feature.id)}
                    className="text-slate-400 hover:text-danger-500"
                    aria-label={t('GlassEnclosure.Designer.WallFeature.Remove', {
                      defaultValue: 'Katmanı sil',
                    })}
                  >
                    <Trash2 size={12} />
                  </button>
                </div>
              );
            })
          )}
        </div>
      )}
    </section>
  );
}

const NumberField = ({
  label,
  value,
  min,
  onCommit,
  onDraft,
}: {
  label: string;
  value: number;
  min?: number;
  onCommit: (value: number) => void;
  onDraft: (value: number) => void;
}) => (
  <label className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
    <span className="text-[10px] uppercase tracking-wide">{label}</span>
    <input
      type="number"
      min={min}
      value={value}
      onChange={(e) => onDraft(Number(e.target.value))}
      onBlur={(e) => onCommit(Number(e.target.value))}
      className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
    />
  </label>
);
