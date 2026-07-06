import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DoorOpen, RectangleHorizontal, Trash2, Wand2 } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { useWallAutofill } from '../hooks/useWallAutofill';
import {
  deriveArcFromRadius,
  deriveArcFromSweep,
  isRealArc,
  minArcRadiusMm,
} from '../model/arcGeometry';
import { queueToast } from '@/shared/api/toastQueue';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  footprintsPenetrate,
  penetratesAny,
} from '../scene/interaction/planCollision';
import { wallFeatureModeLabelKey, wallFeatureShapeLabelKey } from '../model/wallFeatureLabels';
import { newOperationId } from '@/shared/lib/operationId';
import type { SceneWallOpening, SceneWallState, WallEdge } from '../model/project.types';
import { ObjectAppearanceSection } from './ObjectAppearanceSection';

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
  const convertWallBendToLegs = useDesignerStore((s) => s.convertWallBendToLegs);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const { autofill } = useWallAutofill();

  const wall = useMemo(
    () => walls.find((w) => w.id === selection.wallId),
    [walls, selection.wallId],
  );
  const runs = useDesignerStore((s) => s.scene.runs);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  // A group sibling (an L-wall's other leg) is exempted from the collision check ONLY where it
  // already touches this wall's standing footprint — a sibling the user grouped but left clear
  // keeps its safety net so an edit can't drive this wall straight through it.
  const obstacles = useMemo(() => {
    const selfFp = wall ? buildWallFootprint(wall, 0, 0, wall.rotationDeg) : null;
    return [
      ...walls
        .filter((w) => {
          if (!wall || w.id === wall.id) return false;
          if (!(wall.groupId && w.groupId === wall.groupId) || !selfFp) return true;
          return !footprintsPenetrate(selfFp, buildWallFootprint(w, 0, 0, w.rotationDeg));
        })
        .map((w) => buildWallFootprint(w, 0, 0, w.rotationDeg)),
      ...runs.map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg)),
      ...slabs.map((s) => buildSlabFootprint(s, 0, 0, s.rotationDeg)),
    ];
  }, [walls, runs, slabs, wall]);
  const [draft, setDraft] = useState(wall);
  const [tracked, setTracked] = useState(wall);
  if (wall !== tracked) {
    setTracked(wall);
    setDraft(wall);
  }

  if (!wall || !draft) return null;

  // Curved walls defer openings / surface features / autofill (#6a): those still assume
  // a straight chord, so their editors are hidden until an arc-aware version lands.
  const isArc = isRealArc(draft.geomArcRadiusMm, draft.geomArcSweepDeg);

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

  const edgeNotches = wall.edgeNotchMm ?? [];
  const addEdgeNotch = () =>
    updateWall(wall.id, {
      edgeNotchMm: [
        ...edgeNotches,
        {
          id: newOperationId(),
          edge: 'top',
          offsetMm: Math.round(wall.lengthMm * 0.25),
          widthMm: Math.round(wall.lengthMm * 0.2),
          depthMm: Math.round(wall.heightMm * 0.15),
        },
      ],
    });
  const updateEdgeNotch = (id: string, patch: Partial<(typeof edgeNotches)[number]>) =>
    updateWall(wall.id, {
      edgeNotchMm: edgeNotches.map((n) => (n.id === id ? { ...n, ...patch } : n)),
    });
  const removeEdgeNotch = (id: string) =>
    updateWall(wall.id, { edgeNotchMm: edgeNotches.filter((n) => n.id !== id) });

  // Curved walls can't carry openings/surface features yet (#6a/#7), so converting a
  // straight wall to an arc drops them rather than leaving orphaned data that vanishes
  // from the 3D view but lingers in the model.
  const commitArc = (sweep: number) => {
    const hasExtras = (wall.openings ?? []).length > 0 || (wall.features ?? []).length > 0;
    commit({
      // CHORD-INVARIANT: lengthMm stays the chord (the fixed span); radius = chord/(2·sin(sweep/2)),
      // so toggling to an arc bows between the two fixed ends without moving them.
      geomArcRadiusMm: deriveArcFromSweep(draft.lengthMm, sweep).radiusMm,
      geomArcSweepDeg: sweep,
      ...(hasExtras ? { openings: [], features: [] } : {}),
    });
    if (hasExtras) {
      queueToast({
        dedupeKey: 'glass-arc-drops-features',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Wall.ArcDropsFeatures', {
          defaultValue: 'Kavise çevirince bu duvarın açıklık/şekilleri kaldırıldı.',
        }),
      });
    }
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

  const handleAddOpening = (kind: SceneWallOpening['kind'], dims?: OpeningDims) => {
    const opening = createOpening(kind, wall.lengthMm, wall.openings ?? [], dims);
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
          className="rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
        >
          {t('GlassEnclosure.Designer.Wall.Delete', { defaultValue: 'Duvarı sil' })}
        </button>
      </header>

      <ObjectAppearanceSection
        colorHex={wall.colorHex}
        materialKey={wall.materialKey}
        onChange={(patch) => updateWall(wall.id, patch)}
      />

      {Boolean(wall.bendAngleDeg && Math.abs(wall.bendAngleDeg) >= 1) && (
        <div className="flex items-center justify-between gap-2 rounded border border-warning-500/50 bg-warning-50 p-2 text-xs text-warning-800 dark:border-warning-500/40 dark:bg-warning-950/30 dark:text-warning-300">
          <span>
            {t('GlassEnclosure.Designer.Bend.SplitHint', {
              defaultValue:
                'L duvar tek parça — ikiye ayırınca her iki taraf bağımsız düzenlenebilir (genişlet, kavis, serbest çizim).',
            })}
          </span>
          <button
            type="button"
            onClick={() => {
              const converted = convertWallBendToLegs(
                wall.id,
                wall.bendAtMm ?? wall.lengthMm / 2,
                wall.bendAngleDeg ?? 0,
              );
              queueToast({
                dedupeKey: 'glass-bend-split',
                variant: converted ? 'success' : 'warning',
                description: converted
                  ? t('GlassEnclosure.Designer.Bend.SplitDone', {
                      defaultValue:
                        'L duvar iki bağımsız duvara ayrıldı — iki taraf da ayrı düzenlenebilir.',
                    })
                  : t('GlassEnclosure.Designer.Bend.SplitBlocked', {
                      defaultValue: 'Bacaklar çok kısa — kıvrım noktası uçlara çok yakın.',
                    }),
              });
            }}
            className="shrink-0 rounded border border-warning-500/60 px-2 py-1 font-medium hover:bg-warning-100 dark:hover:bg-warning-900/40"
          >
            {t('GlassEnclosure.Designer.Bend.SplitAction', { defaultValue: 'İkiye ayır' })}
          </button>
        </div>
      )}

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

      {/* Fillet + notch modify the flat developed rectangle; a curved (arc) wall is a single annular
          band where they aren't applied yet, so hide them (like the edge-notch gate below). */}
      {!isArc && (
        <>
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
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Designer.CornerNotch.Title', {
                defaultValue: 'Köşe girintisi (mm)',
              })}
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
                  value={wall.cornerNotchMm?.[key] ?? 0}
                  onCommit={(v) =>
                    commit({
                      cornerNotchMm: {
                        ...wall.cornerNotchMm,
                        [key]: Math.max(0, Math.round(v)),
                      },
                    })
                  }
                />
              ))}
            </div>
          </div>
        </>
      )}

      {!isArc && (
        <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
          <div className="flex items-center justify-between">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Designer.EdgeNotch.Title', {
                defaultValue: 'Kenar girintileri (üst/alt/yan yüz)',
              })}
            </p>
            <button
              type="button"
              onClick={addEdgeNotch}
              className="rounded bg-primary-600 px-2 py-0.5 text-xs font-medium text-white hover:bg-primary-700"
            >
              {t('GlassEnclosure.Designer.EdgeNotch.Add', { defaultValue: 'Ekle' })}
            </button>
          </div>
          {edgeNotches.length === 0 ? (
            <p className="text-[10px] leading-snug text-slate-400 dark:text-slate-500">
              {t('GlassEnclosure.Designer.EdgeNotch.Hint', {
                defaultValue:
                  'Bir kenardan içeri dikdörtgen girinti aç (ön/arka + o kenarın yüzünden görünür).',
              })}
            </p>
          ) : (
            edgeNotches.map((notch) => (
              <div key={notch.id} className="flex items-end gap-1.5">
                <label className="flex flex-1 flex-col gap-0.5 text-[10px] text-slate-500 dark:text-slate-400">
                  {t('GlassEnclosure.Designer.EdgeNotch.Edge', { defaultValue: 'Kenar' })}
                  <select
                    value={notch.edge}
                    onChange={(e) =>
                      updateEdgeNotch(notch.id, { edge: e.target.value as WallEdge })
                    }
                    className="rounded border border-slate-300 bg-white px-1 py-0.5 text-xs dark:border-slate-600 dark:bg-slate-800"
                  >
                    {(['top', 'bottom', 'left', 'right'] as const).map((edge) => (
                      <option key={edge} value={edge}>
                        {t(`GlassEnclosure.Designer.Edge.${edge}` as never, {
                          defaultValue: { top: 'Üst', bottom: 'Alt', left: 'Sol', right: 'Sağ' }[
                            edge
                          ],
                        })}
                      </option>
                    ))}
                  </select>
                </label>
                {(
                  [
                    [
                      'offsetMm',
                      t('GlassEnclosure.Designer.EdgeNotch.Offset', { defaultValue: 'Konum' }),
                    ],
                    [
                      'widthMm',
                      t('GlassEnclosure.Designer.EdgeNotch.Width', { defaultValue: 'En' }),
                    ],
                    [
                      'depthMm',
                      t('GlassEnclosure.Designer.EdgeNotch.Depth', { defaultValue: 'Derinlik' }),
                    ],
                  ] as const
                ).map(([field, fieldLabel]) => (
                  <label
                    key={field}
                    className="flex w-14 flex-col gap-0.5 text-[10px] text-slate-500 dark:text-slate-400"
                  >
                    {fieldLabel}
                    <input
                      type="number"
                      min={0}
                      value={notch[field]}
                      onChange={(e) =>
                        updateEdgeNotch(notch.id, {
                          [field]: Math.max(0, Math.round(Number(e.target.value))),
                        })
                      }
                      className="rounded border border-slate-300 bg-white px-1 py-0.5 text-xs dark:border-slate-600 dark:bg-slate-800"
                    />
                  </label>
                ))}
                <button
                  type="button"
                  onClick={() => removeEdgeNotch(notch.id)}
                  aria-label={t('GlassEnclosure.Designer.EdgeNotch.Remove', {
                    defaultValue: 'Sil',
                  })}
                  className="mb-0.5 rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-600 dark:hover:bg-rose-950"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            ))
          )}
        </div>
      )}

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Wall.ArcTitle', { defaultValue: 'Kavis (yay)' })}
        </p>
        <div className="grid grid-cols-3 gap-1.5">
          {(
            [
              ['straight', null],
              ['left', 90],
              ['right', -90],
            ] as const
          ).map(([key, sweep]) => {
            const curved = isArc;
            const side = (draft.geomArcSweepDeg ?? 1) >= 0 ? 'left' : 'right';
            const active = key === 'straight' ? !curved : curved && side === key;
            return (
              <button
                key={key}
                type="button"
                onClick={() =>
                  key === 'straight'
                    ? commit({ geomArcRadiusMm: null, geomArcSweepDeg: null })
                    : commitArc(sweep ?? 90)
                }
                className={`rounded border px-2 py-1.5 text-xs font-medium transition ${
                  active
                    ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-950/40 dark:text-primary-300'
                    : 'border-slate-300 text-slate-600 hover:border-slate-400 dark:border-slate-600 dark:text-slate-300'
                }`}
              >
                {t(`GlassEnclosure.Designer.Wall.Arc.${key}` as never, {
                  defaultValue: { straight: 'Düz', left: 'Sol kavis', right: 'Sağ kavis' }[key],
                })}
              </button>
            );
          })}
        </div>
        {isArc && (
          <NumberField
            label={`${t('GlassEnclosure.Designer.Wall.ArcRadius', { defaultValue: 'Kavis yarıçapı' })} (mm)`}
            value={draft.geomArcRadiusMm ?? draft.lengthMm}
            min={minArcRadiusMm(draft.lengthMm)}
            onCommit={(v) => {
              // Setting the radius keeps the glass length (lengthMm) fixed and re-derives the sweep
              // (= arcLength/radius); the tightest radius is a full circle (arcLength/2π). Sign kept.
              const sign = (draft.geomArcSweepDeg ?? 1) < 0 ? -1 : 1;
              const next = deriveArcFromRadius(
                draft.lengthMm,
                Math.max(minArcRadiusMm(draft.lengthMm), v),
              );
              commit({
                geomArcRadiusMm: next.radiusMm,
                geomArcSweepDeg: sign * (Math.round(next.sweepDeg * 10) / 10),
              });
            }}
            onDraft={(v) => setDraft({ ...draft, geomArcRadiusMm: v })}
          />
        )}
      </div>

      {!isArc && (
        <>
          <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
            <div className="flex items-center justify-between">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Designer.Wall.Openings', { defaultValue: 'Boşluklar' })}
              </p>
              <div className="flex gap-1.5">
                <button
                  type="button"
                  onClick={() => handleAddOpening('window')}
                  className="inline-flex items-center gap-1 rounded border border-primary-500/40 px-2 py-0.5 text-xs text-primary-600 hover:bg-primary-50 dark:hover:bg-primary-950/30"
                >
                  <RectangleHorizontal size={12} />
                  {t('GlassEnclosure.Designer.Wall.AddWindow', { defaultValue: 'Pencere' })}
                </button>
                <button
                  type="button"
                  onClick={() => handleAddOpening('door')}
                  className="inline-flex items-center gap-1 rounded border border-primary-500/40 px-2 py-0.5 text-xs text-primary-600 hover:bg-primary-50 dark:hover:bg-primary-950/30"
                >
                  <DoorOpen size={12} />
                  {t('GlassEnclosure.Designer.Wall.AddDoor', { defaultValue: 'Kapı' })}
                </button>
              </div>
            </div>
            <select
              aria-label={t('GlassEnclosure.Designer.Wall.OpeningPresets', {
                defaultValue: 'Hazır ölçü ekle',
              })}
              value=""
              onChange={(e) => {
                const preset = OPENING_PRESETS[Number(e.target.value)];
                if (preset) handleAddOpening(preset.kind, preset);
              }}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-600 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-300"
            >
              <option value="">
                +{' '}
                {t('GlassEnclosure.Designer.Wall.OpeningPresets', {
                  defaultValue: 'Hazır ölçü ekle',
                })}
              </option>
              {OPENING_PRESETS.map((preset, i) => {
                const kindLabel =
                  preset.kind === 'door'
                    ? t('GlassEnclosure.Designer.Wall.Door', { defaultValue: 'Kapı' })
                    : t('GlassEnclosure.Designer.Wall.Window', { defaultValue: 'Pencere' });
                return (
                  <option key={`${preset.kind}-${preset.widthMm}x${preset.heightMm}`} value={i}>
                    {kindLabel} {preset.widthMm}×{preset.heightMm}
                  </option>
                );
              })}
            </select>
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
                      className="text-slate-400 hover:text-danger-500"
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
        </>
      )}

      {/* The features list stays visible on ARC walls too: curved bands carve pen shapes via CSG
          but render no on-surface selection proxies yet, so this list is the ONLY way to select
          or delete a feature drawn on a curved wall. */}
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
                  className="min-w-0 flex-1 truncate text-left text-[11px] font-medium text-slate-600 hover:text-primary-600 dark:text-slate-300 dark:hover:text-primary-400"
                >
                  {t(shapeLabel.key, { defaultValue: shapeLabel.fallback })} ·{' '}
                  {t(modeLabel.key, { defaultValue: modeLabel.fallback })} · {feature.widthMm}×
                  {feature.heightMm}
                </button>
                <button
                  type="button"
                  onClick={() => removeWallFeature(wall.id, feature.id)}
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

      {!isArc && (
        <>
          <button
            type="button"
            onClick={() => void handleAutofill()}
            className="inline-flex items-center justify-center gap-1.5 rounded-md bg-primary-600 px-3 py-2 text-sm font-medium text-white hover:bg-primary-700"
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
        </>
      )}
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

interface OpeningDims {
  widthMm: number;
  heightMm: number;
  sillMm: number;
}

const OPENING_PRESETS: ({ kind: SceneWallOpening['kind'] } & OpeningDims)[] = [
  { kind: 'door', widthMm: 900, heightMm: 2100, sillMm: 0 },
  { kind: 'door', widthMm: 800, heightMm: 2000, sillMm: 0 },
  { kind: 'door', widthMm: 1800, heightMm: 2100, sillMm: 0 },
  { kind: 'window', widthMm: 1200, heightMm: 1200, sillMm: 900 },
  { kind: 'window', widthMm: 1500, heightMm: 1200, sillMm: 900 },
  { kind: 'window', widthMm: 600, heightMm: 600, sillMm: 1200 },
  { kind: 'window', widthMm: 2400, heightMm: 2400, sillMm: 0 },
];

const createOpening = (
  kind: SceneWallOpening['kind'],
  wallLengthMm: number,
  existing: SceneWallOpening[],
  dims?: OpeningDims,
): SceneWallOpening | null => {
  const base =
    dims ??
    (kind === 'door'
      ? { sillMm: 0, widthMm: 900, heightMm: 2100 }
      : { sillMm: 900, widthMm: 1200, heightMm: 1200 });
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
        className="w-full rounded border border-slate-300 bg-white px-1.5 py-0.5 text-xs text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
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
      className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
    />
  </label>
);
