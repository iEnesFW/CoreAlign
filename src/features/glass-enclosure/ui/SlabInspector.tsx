import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { hasEdgeArc } from '../model/edgeArcOutline';
import { queueToast } from '@/shared/api/toastQueue';
import { useDesignerStore } from '../model/designerStore';
import { useSlabEntityActions } from '../hooks/useDesignerEntityActions';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  penetratesAny,
} from '../scene/interaction/planCollision';
import { wallFeatureModeLabelKey, wallFeatureShapeLabelKey } from '../model/wallFeatureLabels';
import { isRealArc } from '../model/arcGeometry';
import { commitArcOrWarn } from '../geometry/arcCommitFeedback';
import type { ArcCommitInput } from '../geometry/arcCommit';
import type { CurvablePose } from '../geometry/curvature';
import { slabArcDefaultSweepSign } from '../scene/builders/curvedSlabGeometry';
import type { EdgeArcKey, EdgeArcMap, SceneSlabState } from '../model/project.types';
import { ObjectAppearanceSection } from './ObjectAppearanceSection';

export function SlabInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  const walls = useDesignerStore((s) => s.scene.walls ?? []);
  const runs = useDesignerStore((s) => s.scene.runs);
  // Floor moves carry the walls/glass/roofs resting on them; commitSlabPatch persists the runs
  // that rode along (server entities — otherwise the next refetch snaps the glass back).
  const { commitSlabPatch: updateSlab } = useSlabEntityActions();
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

  // Plan-arc slabs now carve + render features (#6b); only barrel/pitched surfaces still defer
  // them. isShapedSurface keeps gating the flat-only geometry knobs (corner fillets).
  const isBarrelOrPitch = (draft.arcRiseMm ?? 0) > 0 || (draft.pitchRiseMm ?? 0) > 0;
  // Every shape the mesh builder resolves BEFORE the filleted-rectangle branch: an edge arc, a
  // plan arc, a barrel or a pitch. In all of those it never reads cornerRadiiMm — so showing the
  // corner controls would offer fields whose value is silently discarded. hasEdgeArc was missing
  // from this gate, which is why the corner inputs stayed visible on a bowed slab.
  const isShapedSurface =
    isRealArc(draft.geomArcRadiusMm, draft.geomArcSweepDeg) ||
    isBarrelOrPitch ||
    hasEdgeArc(draft.geomEdgeArc);
  const planArcAxis = draft.slabArcAxis ?? 'length';
  const planArcChordMm = planArcAxis === 'length' ? draft.lengthMm : draft.depthMm;
  // Preserve an existing curve's side; a FRESH inspector-entered curve defaults to the slab's own
  // body side (the sweep sign is axis-relative — see slabArcDirSign).
  const planArcSign = isRealArc(draft.geomArcRadiusMm, draft.geomArcSweepDeg)
    ? (draft.geomArcSweepDeg ?? 1) < 0
      ? -1
      : 1
    : slabArcDefaultSweepSign(planArcAxis);

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

  // Shaped (curved/pitched) slabs can't carry surface features yet (#6b); converting drops
  // them rather than leaving data that vanishes from the 3D view but lingers. Curve and pitch
  // are mutually exclusive profiles, so turning one on clears the other.
  const commitArcRise = (v: number) => {
    const rise = v > 0 ? Math.round(v) : null;
    const dropFeatures = rise !== null && (slab.features ?? []).length > 0;
    commit({
      arcRiseMm: rise,
      // WHY: the plan-arc profile must clear too — coexisting profiles keep rendering the plan
      // arc while the barrel gating hides every handle (silent dead-end state).
      ...(rise !== null ? { pitchRiseMm: null, geomArcRadiusMm: null, geomArcSweepDeg: null } : {}),
      ...(dropFeatures ? { features: [] } : {}),
    });
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

  const commitPitchRise = (v: number) => {
    const rise = v > 0 ? Math.round(v) : null;
    const dropFeatures = rise !== null && (slab.features ?? []).length > 0;
    commit({
      pitchRiseMm: rise,
      ...(rise !== null ? { arcRiseMm: null, geomArcRadiusMm: null, geomArcSweepDeg: null } : {}),
      ...(dropFeatures ? { features: [] } : {}),
    });
    if (dropFeatures) {
      queueToast({
        dedupeKey: 'glass-pitch-drops-features',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Slab.PitchDropsFeatures', {
          defaultValue: 'Eğimli çatıya çevirince bu yüzeyin şekilleri kaldırıldı.',
        }),
      });
    }
  };

  // Plan arc (curves like a wall) — mutually exclusive with barrel/pitch (the up-curve). Radius/
  // sweep set the curve while the bent axis' two ends stay fixed (symmetric, no rotation).
  // Features SURVIVE (#6b): the arc branch carves + renders them in the developed (s,c) frame.
  // Every curvature edit routes through the shared writer with the SYMMETRIC pose (rotationDeg is
  // the axis, not a rolled tangent — the mesh builder owns the mirror). It supplies the quantised
  // sweep, the chord-consistent radius and the server-radius guard the drag path already had.
  const planArcPose = (chordMm = planArcChordMm): CurvablePose => ({
    lengthMm: chordMm,
    rotationDeg: draft.rotationDeg,
    geomArcRadiusMm: draft.geomArcRadiusMm,
    geomArcSweepDeg: draft.geomArcSweepDeg,
  });
  const applyPlanArc = (
    input: ArcCommitInput,
    extra: Partial<SceneSlabState> = {},
    chordMm = planArcChordMm,
  ) => {
    const patch = commitArcOrWarn(planArcPose(chordMm), input, t, {
      pose: 'symmetric',
      bulge: planArcSign,
    });
    if (!patch) return false;
    commit({
      geomArcRadiusMm: patch.geomArcRadiusMm,
      geomArcSweepDeg: patch.geomArcSweepDeg,
      ...(patch.geomArcRadiusMm === null
        ? {}
        : { slabArcAxis: planArcAxis, arcRiseMm: null, pitchRiseMm: null }),
      ...extra,
    });
    return true;
  };
  const commitPlanArcRadius = (v: number) => {
    applyPlanArc(v > 0 ? { kind: 'radius', radiusMm: v } : { kind: 'straighten' });
  };
  const commitPlanArcSweep = (v: number) => {
    if (v <= 0) return;
    applyPlanArc({ kind: 'sweep', sweepDeg: v });
  };
  const setPlanArcAxis = (axis: 'length' | 'depth') => {
    if (isRealArc(draft.geomArcRadiusMm, draft.geomArcSweepDeg)) {
      // Keep the curl angle; re-derive the radius for the new axis' chord so the ends stay on it.
      // The sweep sign is AXIS-relative, so carry the body-relative side over to the new axis
      // (raw sign reuse would flip which side of the slab the bow lands on). Features live in the
      // developed (s = along the bend, c = across) frame — swapping the bend axis swaps their
      // coordinates so each shape stays on the same physical spot of the sheet.
      const chord = axis === 'length' ? draft.lengthMm : draft.depthMm;
      const onBodySide = planArcSign === slabArcDefaultSweepSign(planArcAxis);
      const nextSign: 1 | -1 = onBodySide
        ? slabArcDefaultSweepSign(axis)
        : slabArcDefaultSweepSign(axis) === 1
          ? -1
          : 1;
      const nextArc = commitArcOrWarn(
        planArcPose(chord),
        { kind: 'chordResize', chordMm: chord },
        t,
        { pose: 'symmetric', bulge: nextSign },
      );
      if (!nextArc) return;
      const swappedFeatures =
        axis !== planArcAxis && (slab.features ?? []).length > 0
          ? {
              features: (slab.features ?? []).map((f) => ({
                ...f,
                offsetMm: f.centerZMm,
                centerZMm: f.offsetMm,
                widthMm: f.heightMm,
                heightMm: f.widthMm,
                points: f.points?.map((p) => ({ x: p.z, z: p.x })),
              })),
            }
          : {};
      commit({
        slabArcAxis: axis,
        geomArcRadiusMm: nextArc.geomArcRadiusMm,
        geomArcSweepDeg: nextArc.geomArcSweepDeg,
        ...swappedFeatures,
      });
    } else {
      commit({ slabArcAxis: axis });
    }
  };

  // Editing the BENT axis' dimension changes the arc's chord: keep the curl angle (sweep) and
  // re-derive the radius so the arc ends stay pinned to the new corners — otherwise the stored
  // radius+sweep imply the OLD chord and the "fixed" bent-edge endpoints visibly shift.
  const commitDimension = (dim: 'lengthMm' | 'depthMm', v: number) => {
    const dimAxis = dim === 'lengthMm' ? 'length' : 'depth';
    if (isRealArc(draft.geomArcRadiusMm, draft.geomArcSweepDeg) && planArcAxis === dimAxis) {
      applyPlanArc({ kind: 'chordResize', chordMm: v }, { [dim]: v }, v);
      return;
    }
    commit({ [dim]: v });
  };

  // Single-edge arc: bow just one rect edge outward. Mutually exclusive with the whole-body
  // arc / barrel / pitch — setting an edge sagitta clears those so the render can't split-brain.
  const commitEdgeArc = (edge: EdgeArcKey, sagittaMm: number) => {
    const base: EdgeArcMap = { ...(slab.geomEdgeArc ?? {}) };
    if (Math.abs(sagittaMm) < 1) delete base[edge];
    else base[edge] = Math.round(sagittaMm);
    const hasAny = Object.values(base).some((s) => typeof s === 'number' && Math.abs(s) >= 1);
    commit({
      geomEdgeArc: hasAny ? base : null,
      ...(hasAny
        ? { geomArcRadiusMm: null, geomArcSweepDeg: null, arcRiseMm: null, pitchRiseMm: null }
        : {}),
    });
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

      <ObjectAppearanceSection
        colorHex={slab.colorHex}
        materialKey={slab.materialKey}
        onChange={(patch) => updateSlab(slab.id, patch)}
      />

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
          onCommit={(v) => commitDimension('lengthMm', v)}
          onDraft={(v) => setDraft({ ...draft, lengthMm: v })}
        />
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.Depth', { defaultValue: 'Derinlik' })} (mm)`}
          value={draft.depthMm}
          min={100}
          onCommit={(v) => commitDimension('depthMm', v)}
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

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Slab.PlanArcTitle', {
            defaultValue: 'Planda kavis (duvar gibi)',
          })}
        </p>
        <div className="flex gap-1.5">
          {(
            [
              [
                'length',
                t('GlassEnclosure.Designer.Slab.PlanArcAxisLength', {
                  defaultValue: 'Uzunluk ekseni',
                }),
              ],
              [
                'depth',
                t('GlassEnclosure.Designer.Slab.PlanArcAxisDepth', {
                  defaultValue: 'Derinlik ekseni',
                }),
              ],
            ] as const
          ).map(([axis, axisLabel]) => (
            <button
              key={axis}
              type="button"
              onClick={() => setPlanArcAxis(axis)}
              className={
                planArcAxis === axis
                  ? 'flex-1 rounded border border-primary-500 bg-primary-50 px-2 py-1 text-xs font-medium text-primary-600 dark:bg-primary-950/30 dark:text-primary-400'
                  : 'flex-1 rounded border border-slate-300 px-2 py-1 text-xs text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800'
              }
            >
              {axisLabel}
            </button>
          ))}
        </div>
        <div className="grid grid-cols-2 gap-2">
          <NumberField
            label={`${t('GlassEnclosure.Field.ArcRadius', { defaultValue: 'Yarıçap' })} (mm)`}
            value={draft.geomArcRadiusMm ?? 0}
            min={0}
            onCommit={(v) => commitPlanArcRadius(v)}
            onDraft={(v) => setDraft({ ...draft, geomArcRadiusMm: v })}
          />
          <NumberField
            label={t('GlassEnclosure.Designer.Arc.SweepInput', { defaultValue: 'Yay açısı (°)' })}
            value={Math.abs(draft.geomArcSweepDeg ?? 0)}
            min={0}
            onCommit={(v) => commitPlanArcSweep(v)}
            onDraft={(v) =>
              setDraft({ ...draft, geomArcSweepDeg: (draft.geomArcSweepDeg ?? 1) < 0 ? -v : v })
            }
          />
        </div>
        <p className="text-[11px] text-slate-400">
          {t('GlassEnclosure.Designer.Slab.PlanArcInfo', {
            defaultValue:
              'Yeşil noktayı sürükle veya değer gir; seçili eksenin iki ucu sabit kalır.',
          })}
        </p>
      </div>

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Slab.BarrelTitle', {
            defaultValue: 'Tonoz (kavisli) yüzey',
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

      <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Slab.PitchTitle', {
            defaultValue: 'Beşik (eğimli) çatı',
          })}
        </p>
        <NumberField
          label={`${t('GlassEnclosure.Designer.Slab.PitchRise', { defaultValue: 'Mahya yüksekliği' })} (mm)`}
          value={draft.pitchRiseMm ?? 0}
          min={0}
          onCommit={(v) => commitPitchRise(v)}
          onDraft={(v) => setDraft({ ...draft, pitchRiseMm: v })}
        />
        {(draft.pitchRiseMm ?? 0) > 0 && (
          <div className="flex gap-1.5">
            {(
              [
                [
                  'symmetric',
                  t('GlassEnclosure.Designer.Slab.PitchSymmetric', {
                    defaultValue: 'Beşik (çift eğim)',
                  }),
                ],
                [
                  'monopitch',
                  t('GlassEnclosure.Designer.Slab.PitchMonopitch', { defaultValue: 'Tek eğim' }),
                ],
              ] as const
            ).map(([type, typeLabel]) => (
              <button
                key={type}
                type="button"
                onClick={() => commit({ pitchType: type })}
                className={
                  (draft.pitchType ?? 'symmetric') === type
                    ? 'flex-1 rounded border border-primary-500 bg-primary-50 px-2 py-1 text-xs font-medium text-primary-600 dark:bg-primary-950/30 dark:text-primary-400'
                    : 'flex-1 rounded border border-slate-300 px-2 py-1 text-xs text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800'
                }
              >
                {typeLabel}
              </button>
            ))}
          </div>
        )}
      </div>

      {!isShapedSurface && (
        <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Designer.Slab.EdgeArcTitle', { defaultValue: 'Tek kenar kavis' })}
          </p>
          <div className="grid grid-cols-2 gap-2">
            {(
              [
                [
                  'front',
                  t('GlassEnclosure.Designer.Slab.EdgeFront', { defaultValue: 'Ön kenar' }),
                ],
                [
                  'back',
                  t('GlassEnclosure.Designer.Slab.EdgeBack', { defaultValue: 'Arka kenar' }),
                ],
                ['left', t('GlassEnclosure.Designer.Slab.EdgeLeft', { defaultValue: 'Sol kenar' })],
                [
                  'right',
                  t('GlassEnclosure.Designer.Slab.EdgeRight', { defaultValue: 'Sağ kenar' }),
                ],
              ] as const
            ).map(([edge, edgeLabel]) => (
              <NumberField
                key={edge}
                label={`${edgeLabel} (mm)`}
                value={draft.geomEdgeArc?.[edge] ?? 0}
                onCommit={(v) => commitEdgeArc(edge, v)}
                onDraft={(v) =>
                  setDraft({ ...draft, geomEdgeArc: { ...(draft.geomEdgeArc ?? {}), [edge]: v } })
                }
              />
            ))}
          </div>
          <p className="text-[11px] text-slate-400">
            {t('GlassEnclosure.Designer.Slab.EdgeArcInfo', {
              defaultValue: 'Bir kenara ok (sagitta) girin; sadece o kenar dışa kavislenir.',
            })}
          </p>
        </div>
      )}

      {!isShapedSurface && (
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
              />
            ))}
          </div>
        </div>
      )}

      {!isBarrelOrPitch && (
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
  /**
   * Optional. Supply it when the PARENT wants to mirror each keystroke (a live 3D preview).
   *
   * WHY optional: this input is fully controlled by `value`, so a caller that passed a no-op
   * `onDraft` produced a field that could not be typed into AT ALL — every keystroke re-rendered
   * the unchanged committed value straight back over it. Omitting it now means "let the field hold
   * its own draft until blur", which is what those callers actually wanted.
   */
  onDraft?: (value: number) => void;
}) => {
  const [draft, setDraft] = useState<string | null>(null);
  return (
    <label className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
      <span className="text-[10px] uppercase tracking-wide">{label}</span>
      <input
        type="number"
        min={min}
        value={draft ?? value}
        onChange={(e) => {
          setDraft(e.target.value);
          onDraft?.(Number(e.target.value));
        }}
        onBlur={(e) => {
          setDraft(null);
          onCommit(Number(e.target.value));
        }}
        onKeyDown={(e) => {
          if (e.key === 'Enter') e.currentTarget.blur();
        }}
        className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
      />
    </label>
  );
};
