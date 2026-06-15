import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DoorOpen, RectangleHorizontal, Trash2, Wand2 } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { useWallAutofill } from '../hooks/useWallAutofill';
import { queueToast } from '@/shared/api/toastQueue';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  penetratesAny,
} from '../scene/interaction/planCollision';
import { wallFeatureModeLabelKey, wallFeatureShapeLabelKey } from '../model/wallFeatureLabels';
import type { SceneWallOpening, SceneWallState } from '../model/project.types';

export function WallInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const walls = useDesignerStore((s) => s.scene.walls ?? []);
  const updateWall = useDesignerStore((s) => s.updateWall);
  const removeWall = useDesignerStore((s) => s.removeWall);
  const addWallOpening = useDesignerStore((s) => s.addWallOpening);
  const updateWallOpening = useDesignerStore((s) => s.updateWallOpening);
  const removeWallOpening = useDesignerStore((s) => s.removeWallOpening);
  const removeWallFeature = useDesignerStore((s) => s.removeWallFeature);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const { autofill } = useWallAutofill();

  const wall = useMemo(
    () => walls.find((w) => w.id === selection.wallId),
    [walls, selection.wallId],
  );
  const runs = useDesignerStore((s) => s.scene.runs);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  const obstacles = useMemo(
    () => [
      ...walls.map((w) => buildWallFootprint(w, 0, 0, w.rotationDeg)),
      ...runs.map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg)),
      ...slabs.map((s) => buildSlabFootprint(s, 0, 0, s.rotationDeg)),
    ],
    [walls, runs, slabs],
  );
  const [draft, setDraft] = useState(wall);
  const [tracked, setTracked] = useState(wall);
  if (wall !== tracked) {
    setTracked(wall);
    setDraft(wall);
  }

  if (!wall || !draft) return null;

  const commit = (patch: Partial<typeof wall>) => {
    const candidate: SceneWallState = { ...wall, ...patch };
    const alreadyColliding = penetratesAny(
      buildWallFootprint(wall, 0, 0, wall.rotationDeg),
      obstacles,
    );
    const wouldCollide = penetratesAny(
      buildWallFootprint(candidate, 0, 0, candidate.rotationDeg),
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
      setDraft(wall);
      return;
    }
    updateWall(wall.id, patch);
  };

  const handleDelete = () => {
    removeWall(wall.id);
    setSelection({
      kind: null,
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
    });
  };

  const handleAddOpening = (kind: SceneWallOpening['kind']) => {
    const opening = createOpening(kind, wall.lengthMm, wall.openings ?? []);
    if (!opening) {
      queueToast({
        dedupeKey: 'glass-wall-opening-full',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Wall.NoRoomForOpening', {
          defaultValue:
            'Duvarda yeni boşluk için yer yok — mevcutları daraltın veya duvarı uzatın.',
        }),
      });
      return;
    }
    addWallOpening(wall.id, opening);
  };

  const handleAutofill = async () => {
    const created = await autofill();
    queueToast({
      dedupeKey: 'glass-wall-autofill',
      variant: created > 0 ? 'success' : 'info',
      description:
        created > 0
          ? t('GlassEnclosure.Designer.Wall.AutofillDone', {
              defaultValue: '{{count}} cam hattı oluşturuldu.',
              count: created,
            })
          : t('GlassEnclosure.Designer.Wall.AutofillNone', {
              defaultValue:
                'Doldurulacak boşluk bulunamadı — duvara pencere/kapı boşluğu veya delik ekleyin.',
            }),
    });
  };

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Designer.Wall.Title', { defaultValue: 'Duvar / Engel' })}
        </h3>
        <button
          type="button"
          onClick={handleDelete}
          className="rounded border border-red-500/40 px-2 py-1 text-xs text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
        >
          {t('GlassEnclosure.Designer.Wall.Delete', { defaultValue: 'Duvarı sil' })}
        </button>
      </header>

      <div className="grid grid-cols-2 gap-2">
        <NumberField
          label={`${t('GlassEnclosure.Field.Length', { defaultValue: 'Uzunluk' })} (mm)`}
          value={draft.lengthMm}
          min={100}
          onCommit={(v) => commit({ lengthMm: v })}
          onDraft={(v) => setDraft({ ...draft, lengthMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Field.Height', { defaultValue: 'Yükseklik' })} (mm)`}
          value={draft.heightMm}
          min={100}
          onCommit={(v) => commit({ heightMm: v })}
          onDraft={(v) => setDraft({ ...draft, heightMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Wall.Thickness', { defaultValue: 'Kalınlık' })} (mm)`}
          value={draft.thicknessMm}
          min={50}
          onCommit={(v) => commit({ thicknessMm: v })}
          onDraft={(v) => setDraft({ ...draft, thicknessMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Field.Rotation', { defaultValue: 'Açı' })} (°)`}
          value={draft.rotationDeg}
          onCommit={(v) => commit({ rotationDeg: v })}
          onDraft={(v) => setDraft({ ...draft, rotationDeg: v })}
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
          label={t('GlassEnclosure.Designer.Wall.HeightEnd', {
            defaultValue: 'Uç yükseklik / eğim (mm)',
          })}
          value={draft.heightEndMm ?? draft.heightMm}
          min={100}
          onCommit={(v) => commit({ heightEndMm: v === draft.heightMm ? null : v })}
          onDraft={(v) => setDraft({ ...draft, heightEndMm: v })}
        />
      </div>

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
            <OpeningField
              key={key}
              label={cornerLabel}
              value={wall.cornerRadiiMm?.[key] ?? 0}
              onCommit={(v) =>
                commit({
                  cornerRadiiMm: {
                    ...wall.cornerRadiiMm,
                    [key]: Math.max(0, Math.round(v)),
                  },
                })
              }
            />
          ))}
        </div>
      </div>

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <div className="flex items-center justify-between">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Designer.Wall.Openings', { defaultValue: 'Boşluklar' })}
          </p>
          <div className="flex gap-1.5">
            <button
              type="button"
              onClick={() => handleAddOpening('window')}
              className="inline-flex items-center gap-1 rounded border border-blue-500/40 px-2 py-0.5 text-xs text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-950/30"
            >
              <RectangleHorizontal size={12} />
              {t('GlassEnclosure.Designer.Wall.AddWindow', { defaultValue: 'Pencere' })}
            </button>
            <button
              type="button"
              onClick={() => handleAddOpening('door')}
              className="inline-flex items-center gap-1 rounded border border-blue-500/40 px-2 py-0.5 text-xs text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-950/30"
            >
              <DoorOpen size={12} />
              {t('GlassEnclosure.Designer.Wall.AddDoor', { defaultValue: 'Kapı' })}
            </button>
          </div>
        </div>
        {(wall.openings ?? []).length === 0 ? (
          <p className="text-[11px] text-slate-400">
            {t('GlassEnclosure.Designer.Wall.NoOpenings', {
              defaultValue: 'Boşluk yok — pencere veya kapı ekleyin.',
            })}
          </p>
        ) : (
          (wall.openings ?? []).map((opening) => (
            <div
              key={opening.id}
              className="space-y-1 rounded border border-slate-200 p-2 dark:border-slate-700"
            >
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-medium text-slate-600 dark:text-slate-300">
                  {opening.kind === 'door'
                    ? t('GlassEnclosure.Designer.Wall.Door', { defaultValue: 'Kapı' })
                    : t('GlassEnclosure.Designer.Wall.Window', { defaultValue: 'Pencere' })}
                </span>
                <button
                  type="button"
                  onClick={() => removeWallOpening(wall.id, opening.id)}
                  className="text-slate-400 hover:text-red-500"
                  aria-label={t('GlassEnclosure.Designer.Wall.RemoveOpening', {
                    defaultValue: 'Boşluğu sil',
                  })}
                >
                  <Trash2 size={12} />
                </button>
              </div>
              <div className="grid grid-cols-4 gap-1.5">
                <OpeningField
                  label={t('GlassEnclosure.Designer.Wall.OpeningOffset', {
                    defaultValue: 'Konum',
                  })}
                  value={opening.offsetMm}
                  onCommit={(v) => updateWallOpening(wall.id, opening.id, { offsetMm: v })}
                />
                <OpeningField
                  label={t('GlassEnclosure.Designer.Wall.OpeningSill', { defaultValue: 'Alt' })}
                  value={opening.sillMm}
                  onCommit={(v) => updateWallOpening(wall.id, opening.id, { sillMm: v })}
                />
                <OpeningField
                  label={t('GlassEnclosure.Field.Width', { defaultValue: 'Genişlik' })}
                  value={opening.widthMm}
                  onCommit={(v) => updateWallOpening(wall.id, opening.id, { widthMm: v })}
                />
                <OpeningField
                  label={t('GlassEnclosure.Field.Height', { defaultValue: 'Yükseklik' })}
                  value={opening.heightMm}
                  onCommit={(v) => updateWallOpening(wall.id, opening.id, { heightMm: v })}
                />
              </div>
            </div>
          ))
        )}
      </div>

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.WallFeature.ListTitle', { defaultValue: 'Katmanlar' })}
        </p>
        {(wall.features ?? []).length === 0 ? (
          <p className="text-[11px] text-slate-400">
            {t('GlassEnclosure.Designer.WallFeature.None', {
              defaultValue: "Katman yok — üstteki 'Yüzeye çiz' aracıyla duvar üzerine şekil çizin.",
            })}
          </p>
        ) : (
          (wall.features ?? []).map((feature) => {
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
                      kind: 'wallFeature',
                      runId: null,
                      panelId: null,
                      connectionId: null,
                      hardwareId: null,
                      wallId: wall.id,
                      slabId: null,
                      featureId: feature.id,
                    })
                  }
                  className="min-w-0 flex-1 truncate text-left text-[11px] font-medium text-slate-600 hover:text-blue-600 dark:text-slate-300 dark:hover:text-blue-400"
                >
                  {t(shapeLabel.key, { defaultValue: shapeLabel.fallback })} ·{' '}
                  {t(modeLabel.key, { defaultValue: modeLabel.fallback })} · {feature.widthMm}×
                  {feature.heightMm}
                </button>
                <button
                  type="button"
                  onClick={() => removeWallFeature(wall.id, feature.id)}
                  className="text-slate-400 hover:text-red-500"
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

      <button
        type="button"
        onClick={() => void handleAutofill()}
        className="inline-flex items-center justify-center gap-1.5 rounded-md bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700"
      >
        <Wand2 size={14} />
        {t('GlassEnclosure.Designer.Wall.Autofill', {
          defaultValue: 'Boşlukları camla doldur',
        })}
      </button>
      <p className="text-[11px] text-slate-400">
        {t('GlassEnclosure.Designer.Wall.AutofillHint', {
          defaultValue:
            'Seçili duvarın pencere/kapı boşluklarını ve deliklerini cam hattıyla doldurur.',
        })}
      </p>
    </section>
  );
}

const OPENING_GAP_MM = 100;
const OPENING_EDGE_MM = 100;

const findFreeOffset = (
  widthMm: number,
  wallLengthMm: number,
  existing: SceneWallOpening[],
): number | null => {
  const half = widthMm / 2;
  const intervals = existing
    .map((o) => [
      o.offsetMm - o.widthMm / 2 - OPENING_GAP_MM,
      o.offsetMm + o.widthMm / 2 + OPENING_GAP_MM,
    ])
    .sort((a, b) => a[0] - b[0]);
  const fits = (center: number) => {
    if (center - half < OPENING_EDGE_MM || center + half > wallLengthMm - OPENING_EDGE_MM)
      return false;
    return intervals.every(([lo, hi]) => center + half <= lo || center - half >= hi);
  };
  if (fits(wallLengthMm / 2)) return Math.round(wallLengthMm / 2);
  for (let step = 100; step <= wallLengthMm; step += 100) {
    if (fits(wallLengthMm / 2 - step)) return Math.round(wallLengthMm / 2 - step);
    if (fits(wallLengthMm / 2 + step)) return Math.round(wallLengthMm / 2 + step);
  }
  return null;
};

const createOpening = (
  kind: SceneWallOpening['kind'],
  wallLengthMm: number,
  existing: SceneWallOpening[],
): SceneWallOpening | null => {
  const base =
    kind === 'door'
      ? { sillMm: 0, widthMm: 900, heightMm: 2100 }
      : { sillMm: 900, widthMm: 1200, heightMm: 1200 };
  const offsetMm = findFreeOffset(base.widthMm, wallLengthMm, existing);
  if (offsetMm === null) return null;
  return { id: crypto.randomUUID(), kind, offsetMm, ...base };
};

const OpeningField = ({
  label,
  value,
  onCommit,
}: {
  label: string;
  value: number;
  onCommit: (value: number) => void;
}) => {
  const [draft, setDraft] = useState(String(value));
  const [tracked, setTracked] = useState(value);
  if (value !== tracked) {
    setTracked(value);
    setDraft(String(value));
  }
  return (
    <label className="flex flex-col gap-0.5 text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
      <span>{label}</span>
      <input
        type="number"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onBlur={() => {
          const parsed = Number(draft);
          if (!Number.isNaN(parsed)) onCommit(parsed);
        }}
        className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-xs text-slate-900 focus:border-blue-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
      />
    </label>
  );
};

export const NumberField = ({
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
      className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-blue-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
    />
  </label>
);
