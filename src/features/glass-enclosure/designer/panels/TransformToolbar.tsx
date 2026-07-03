import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import {
  usePanelEntityActions,
  useRunEntityActions,
} from '@/features/glass-enclosure/hooks/useDesignerEntityActions';
import { snapAngleDeg } from '@/features/glass-enclosure/model/angleSnap';
import { queueToast } from '@/shared/api/toastQueue';
import type { PlanFootprint } from '@/shared/three-engine';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildWallFootprint,
  footprintsPenetrate,
  penetratesAny,
} from '@/features/glass-enclosure/scene/interaction/planCollision';
import {
  findAttachedRunIds,
  findAttachedWallIds,
} from '@/features/glass-enclosure/model/wallAttachment';
import { arcFromCornerResize, isRealArc } from '@/features/glass-enclosure/model/arcGeometry';
import type {
  SceneHardwareItem,
  ScenePanelState,
  SceneRunState,
  SceneSlabState,
  SceneWallState,
} from '@/features/glass-enclosure/model/project.types';

const clampMm = (value: number, limit: number) => Math.min(limit, Math.max(-limit, value));

const solidObstaclesExcept = (excludeIds: Set<string>): PlanFootprint[] => {
  const s = useDesignerStore.getState().scene;
  return [
    ...(s.walls ?? [])
      .filter((w) => !excludeIds.has(w.id))
      .map((w) => buildWallFootprint(w, 0, 0, w.rotationDeg)),
    ...s.runs
      .filter((r) => !excludeIds.has(r.id))
      .map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg)),
    ...(s.slabs ?? [])
      .filter((sl) => !excludeIds.has(sl.id))
      .map((sl) => buildSlabFootprint(sl, 0, 0, sl.rotationDeg)),
  ];
};

const transformAllowed = (
  currentFp: PlanFootprint,
  candidateFp: PlanFootprint,
  obstacles: PlanFootprint[],
  blockedMessage: string,
): boolean => {
  const fresh = obstacles.filter(
    (o) => o.ownerId !== currentFp.ownerId && !footprintsPenetrate(currentFp, o),
  );
  if (!penetratesAny(candidateFp, fresh)) return true;
  queueToast({
    dedupeKey: 'glass-collision-blocked',
    variant: 'warning',
    description: blockedMessage,
  });
  return false;
};

interface NumericFieldProps {
  label: string;
  unit: string;
  value: number;
  onCommit: (value: number) => void;
}

const NumericField = ({ label, unit, value, onCommit }: NumericFieldProps) => {
  const [draft, setDraft] = useState(String(value));
  const [tracked, setTracked] = useState(value);
  if (value !== tracked) {
    setTracked(value);
    setDraft(String(value));
  }

  const commit = () => {
    const parsed = Number(draft);
    if (!Number.isFinite(parsed)) {
      setDraft(String(value));
      return;
    }
    onCommit(parsed);
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') e.currentTarget.blur();
  };

  return (
    <label className="flex flex-col gap-0.5">
      <span className="text-[10px] font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {label} ({unit})
      </span>
      <input
        type="number"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onBlur={commit}
        onKeyDown={handleKeyDown}
        className="h-7 w-16 rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
        aria-label={`${label} (${unit})`}
      />
    </label>
  );
};

const RunFields = ({ run }: { run: SceneRunState }) => {
  const { t } = useTranslation();
  const updateRun = useDesignerStore((s) => s.updateRun);
  const { persistRun } = useRunEntityActions();
  const { persistPanel } = usePanelEntityActions();

  const commit = (patch: Partial<SceneRunState>) => {
    // WHY: on an ARC run a typed length is a keep-sweep chord resize — persisting the new chord
    // with the STALE radius made projectToScene revert it to the old chord on the next refetch.
    const effective =
      patch.lengthMm !== undefined && isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)
        ? {
            ...patch,
            geomArcRadiusMm: arcFromCornerResize(patch.lengthMm, run.geomArcSweepDeg ?? 1)
              .geomArcRadiusMm,
          }
        : patch;
    const candidate = { ...run, ...effective };
    const attached = findAttachedWallIds(run, useDesignerStore.getState().scene.walls ?? []);
    const obstacles = solidObstaclesExcept(new Set([run.id, ...attached]));
    if (
      !transformAllowed(
        buildRunFootprint(run, 0, 0, run.rotationDeg),
        buildRunFootprint(candidate, 0, 0, candidate.rotationDeg),
        obstacles,
        t('GlassEnclosure.Designer.CollisionBlocked', {
          defaultValue: 'Bu değer başka bir nesneyle çakışıyor — uygulanmadı.',
        }),
      )
    ) {
      return;
    }
    const beforeWidths = new Map(run.panels.map((p) => [p.id, p.widthMm]));
    updateRun(run.id, effective);
    const freshRun = useDesignerStore.getState().scene.runs.find((r) => r.id === run.id);
    void persistRun(freshRun ?? candidate);
    for (const p of freshRun?.panels ?? []) {
      if (beforeWidths.get(p.id) !== p.widthMm) void persistPanel(run.id, p);
    }
  };

  return (
    <>
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Length', { defaultValue: 'Uzunluk' })}
        unit="mm"
        value={run.lengthMm}
        onCommit={(v) => commit({ lengthMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Height', { defaultValue: 'Yükseklik' })}
        unit="mm"
        value={run.heightMm}
        onCommit={(v) => commit({ heightMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.X', { defaultValue: 'X' })}
        unit="mm"
        value={run.originX}
        onCommit={(v) => commit({ originX: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Y', { defaultValue: 'Y' })}
        unit="mm"
        value={run.originY}
        onCommit={(v) => commit({ originY: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Rotation', { defaultValue: 'Dönüş' })}
        unit="°"
        value={run.rotationDeg}
        onCommit={(v) => commit({ rotationDeg: snapAngleDeg(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Base', { defaultValue: 'Taban' })}
        unit="mm"
        value={run.geomZ ?? 0}
        onCommit={(v) => commit({ geomZ: Math.round(v) })}
      />
    </>
  );
};

const PanelFields = ({ run, panel }: { run: SceneRunState; panel: ScenePanelState }) => {
  const { t } = useTranslation();
  const updatePanel = useDesignerStore((s) => s.updatePanel);
  const { persistPanel } = usePanelEntityActions();
  const { persistRun } = useRunEntityActions();

  const commitWidth = (value: number) => {
    // Persist the STORE's post-commit state: on an ARC run the width is pinned/clamped and the
    // sibling widths are REDISTRIBUTED (pinPanelWidth) — persisting the raw value would leave the
    // server with stale siblings (Σ ≠ the developed length).
    const widthMm = Math.max(100, Math.round(value));
    const beforeWidths = new Map(run.panels.map((p) => [p.id, p.widthMm]));
    updatePanel(run.id, panel.id, { widthMm });
    const freshRun = useDesignerStore.getState().scene.runs.find((r) => r.id === run.id);
    const freshPanel = freshRun?.panels.find((p) => p.id === panel.id);
    void persistPanel(run.id, freshPanel ?? { ...panel, widthMm });
    if (freshRun) {
      freshRun.panels.forEach((p) => {
        if (p.id !== panel.id && beforeWidths.get(p.id) !== p.widthMm) {
          void persistPanel(run.id, p);
        }
      });
      void persistRun(freshRun);
    }
  };

  return (
    <NumericField
      label={t('GlassEnclosure.Designer.Transform.Width', { defaultValue: 'Genişlik' })}
      unit="mm"
      value={panel.widthMm}
      onCommit={commitWidth}
    />
  );
};

const WallFields = ({ wall }: { wall: SceneWallState }) => {
  const { t } = useTranslation();
  const updateWall = useDesignerStore((s) => s.updateWall);

  const commit = (patch: Partial<SceneWallState>) => {
    const candidate = { ...wall, ...patch };
    const attached = findAttachedRunIds(wall, useDesignerStore.getState().scene.runs);
    const obstacles = solidObstaclesExcept(new Set([wall.id, ...attached]));
    if (
      !transformAllowed(
        buildWallFootprint(wall, 0, 0, wall.rotationDeg),
        buildWallFootprint(candidate, 0, 0, candidate.rotationDeg),
        obstacles,
        t('GlassEnclosure.Designer.CollisionBlocked', {
          defaultValue: 'Bu değer başka bir nesneyle çakışıyor — uygulanmadı.',
        }),
      )
    ) {
      return;
    }
    updateWall(wall.id, patch);
  };

  return (
    <>
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Length', { defaultValue: 'Uzunluk' })}
        unit="mm"
        value={wall.lengthMm}
        onCommit={(v) => commit({ lengthMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Height', { defaultValue: 'Yükseklik' })}
        unit="mm"
        value={wall.heightMm}
        onCommit={(v) => commit({ heightMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.HeightEnd', { defaultValue: 'Uç Yükseklik' })}
        unit="mm"
        value={wall.heightEndMm ?? wall.heightMm}
        onCommit={(v) => commit({ heightEndMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Thickness', { defaultValue: 'Kalınlık' })}
        unit="mm"
        value={wall.thicknessMm}
        onCommit={(v) => commit({ thicknessMm: Math.max(10, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.X', { defaultValue: 'X' })}
        unit="mm"
        value={wall.originX}
        onCommit={(v) => commit({ originX: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Y', { defaultValue: 'Y' })}
        unit="mm"
        value={wall.originY}
        onCommit={(v) => commit({ originY: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Rotation', { defaultValue: 'Dönüş' })}
        unit="°"
        value={wall.rotationDeg}
        onCommit={(v) => commit({ rotationDeg: Math.round(v) })}
      />
    </>
  );
};

const SlabFields = ({ slab }: { slab: SceneSlabState }) => {
  const { t } = useTranslation();
  const updateSlab = useDesignerStore((s) => s.updateSlab);

  const commit = (patch: Partial<SceneSlabState>) => {
    const candidate = { ...slab, ...patch };
    const obstacles = solidObstaclesExcept(new Set([slab.id]));
    if (
      !transformAllowed(
        buildSlabFootprint(slab, 0, 0, slab.rotationDeg),
        buildSlabFootprint(candidate, 0, 0, candidate.rotationDeg),
        obstacles,
        t('GlassEnclosure.Designer.CollisionBlocked', {
          defaultValue: 'Bu değer başka bir nesneyle çakışıyor — uygulanmadı.',
        }),
      )
    ) {
      return;
    }
    updateSlab(slab.id, patch);
  };

  return (
    <>
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Length', { defaultValue: 'Uzunluk' })}
        unit="mm"
        value={slab.lengthMm}
        onCommit={(v) => commit({ lengthMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Depth', { defaultValue: 'Derinlik' })}
        unit="mm"
        value={slab.depthMm}
        onCommit={(v) => commit({ depthMm: Math.max(100, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Thickness', { defaultValue: 'Kalınlık' })}
        unit="mm"
        value={slab.thicknessMm}
        onCommit={(v) => commit({ thicknessMm: Math.max(10, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Elevation', { defaultValue: 'Kot' })}
        unit="mm"
        value={slab.elevationMm}
        onCommit={(v) => commit({ elevationMm: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.X', { defaultValue: 'X' })}
        unit="mm"
        value={slab.originX}
        onCommit={(v) => commit({ originX: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Y', { defaultValue: 'Y' })}
        unit="mm"
        value={slab.originY}
        onCommit={(v) => commit({ originY: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Rotation', { defaultValue: 'Dönüş' })}
        unit="°"
        value={slab.rotationDeg}
        onCommit={(v) => commit({ rotationDeg: Math.round(v) })}
      />
    </>
  );
};

const HardwareFields = ({
  run,
  panel,
  item,
}: {
  run: SceneRunState;
  panel: ScenePanelState;
  item: SceneHardwareItem;
}) => {
  const { t } = useTranslation();
  const updateHardware = useDesignerStore((s) => s.updateHardware);

  const commit = (patch: Partial<SceneHardwareItem>) => {
    const next = { ...item, ...patch };
    const edgeX = Math.max(0, panel.widthMm / 2 - next.widthMm / 2);
    const edgeY = Math.max(0, run.heightMm / 2 - next.heightMm / 2);
    updateHardware(run.id, panel.id, item.id, {
      ...patch,
      offsetXmm: clampMm(next.offsetXmm, edgeX),
      offsetYmm: clampMm(next.offsetYmm, edgeY),
    });
  };

  return (
    <>
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Width', { defaultValue: 'Genişlik' })}
        unit="mm"
        value={item.widthMm}
        onCommit={(v) => commit({ widthMm: Math.max(1, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Height', { defaultValue: 'Yükseklik' })}
        unit="mm"
        value={item.heightMm}
        onCommit={(v) => commit({ heightMm: Math.max(1, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Depth', { defaultValue: 'Derinlik' })}
        unit="mm"
        value={item.depthMm}
        onCommit={(v) => commit({ depthMm: Math.max(1, Math.round(v)) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.X', { defaultValue: 'X' })}
        unit="mm"
        value={item.offsetXmm}
        onCommit={(v) => commit({ offsetXmm: Math.round(v) })}
      />
      <NumericField
        label={t('GlassEnclosure.Designer.Transform.Y', { defaultValue: 'Y' })}
        unit="mm"
        value={item.offsetYmm}
        onCommit={(v) => commit({ offsetYmm: Math.round(v) })}
      />
    </>
  );
};

export function TransformToolbar() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);
  const walls = useDesignerStore((s) => s.scene.walls);
  const slabs = useDesignerStore((s) => s.scene.slabs);

  const run = runs.find((r) => r.id === selection.runId);
  const panel = run?.panels.find((p) => p.id === selection.panelId);
  const hardware = panel?.hardware.find((h) => h.id === selection.hardwareId);
  const wall = (walls ?? []).find((w) => w.id === selection.wallId);
  const slab = (slabs ?? []).find((s) => s.id === selection.slabId);

  const renderFields = () => {
    if (selection.kind === 'run' && run) return <RunFields key={run.id} run={run} />;
    if (selection.kind === 'panel' && run && panel)
      return <PanelFields key={panel.id} run={run} panel={panel} />;
    if (selection.kind === 'hardware' && run && panel && hardware)
      return <HardwareFields key={hardware.id} run={run} panel={panel} item={hardware} />;
    if (selection.kind === 'wall' && wall) return <WallFields key={wall.id} wall={wall} />;
    if (selection.kind === 'slab' && slab) return <SlabFields key={slab.id} slab={slab} />;
    return null;
  };

  const fields = renderFields();
  if (!fields) return null;

  return (
    <div className="pointer-events-none absolute bottom-3 left-1/2 z-10 w-max max-w-[calc(100%-200px)] -translate-x-1/2">
      <div
        role="toolbar"
        aria-label={t('GlassEnclosure.Designer.Transform.Title', { defaultValue: 'Dönüşüm' })}
        className="pointer-events-auto flex flex-wrap items-end justify-center gap-x-2 gap-y-1 rounded-lg border border-slate-200 bg-white/90 px-2.5 py-1.5 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/90"
      >
        {fields}
      </div>
    </div>
  );
}

export default TransformToolbar;
