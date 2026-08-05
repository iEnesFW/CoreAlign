import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlignCenterHorizontal,
  AlignCenterVertical,
  AlignHorizontalSpaceAround,
  AlignVerticalSpaceAround,
  Copy,
  FlipHorizontal2,
  Group as GroupIcon,
  Home,
  Link2,
  Lock,
  LockOpen,
  Plus,
  Ruler,
  Spline,
  Trash2,
  Ungroup,
  UnfoldVertical,
  Wand2,
} from 'lucide-react';
import type { CornerFillMode } from '@/features/glass-enclosure/model/multiAutofill';
import { bodyChordVectorMm } from '@/features/glass-enclosure/geometry/curvature';
import {
  mirrorSlabPatch,
  mirrorSurfacePatch,
  mirrorWallPatch,
} from '@/features/glass-enclosure/model/mirrorBody';
import { dropLockedIds, lockedBodyIds } from '@/features/glass-enclosure/model/sceneGuards';
import { notifyLockedBlocked } from '@/features/glass-enclosure/model/lockFeedback';
import { cn } from '@/shared/lib/cn';
import { queueToast } from '@/shared/api/toastQueue';
import type { PlanFootprint } from '@/shared/three-engine';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildSurfaceFootprint,
  buildWallFootprint,
  penetratesAny,
} from '@/features/glass-enclosure/scene/interaction/planCollision';
import {
  computeRoofOverWalls,
  computeRoofSurfaceOverWalls,
} from '@/features/glass-enclosure/model/roofFromWalls';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import {
  useDesignerEntityActions,
  useWallEntityActions,
} from '@/features/glass-enclosure/hooks/useDesignerEntityActions';
import { useMultiSelectionDelete } from '@/features/glass-enclosure/hooks/useMultiSelectionDelete';
import { useSlabEntityActions } from '@/features/glass-enclosure/hooks/useDesignerEntityActions';
import { useMultiAlignActions } from '@/features/glass-enclosure/hooks/useMultiAlignActions';
import { useWallAutofill } from '@/features/glass-enclosure/hooks/useWallAutofill';
import type { GlassTypeDto } from '@/features/glass-enclosure/model/glassEnclosure.types';

interface SelectionToolbarProps {
  glassTypes: GlassTypeDto[];
}

const ARRAY_COUNT = 3;
const ARRAY_GAP_MM = 200;

const CORNER_FILL_ORDER: CornerFillMode[] = ['auto', 'straight', 'L', 'arc'];

export function SelectionToolbar({ glassTypes }: SelectionToolbarProps) {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);
  const addHardware = useDesignerStore((s) => s.addHardware);
  const removeHardware = useDesignerStore((s) => s.removeHardware);
  const removeConnection = useDesignerStore((s) => s.removeConnection);
  const removeWall = useDesignerStore((s) => s.removeWall);
  const removeWallFeature = useDesignerStore((s) => s.removeWallFeature);
  const removeSlabFeature = useDesignerStore((s) => s.removeSlabFeature);
  const removeSurface = useDesignerStore((s) => s.removeSurface);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  const surfaces = useDesignerStore((s) => s.scene.surfaces ?? []);
  const walls = useDesignerStore((s) => s.scene.walls ?? []);
  const removeSlab = useDesignerStore((s) => s.removeSlab);
  const addSlab = useDesignerStore((s) => s.addSlab);
  const addSurface = useDesignerStore((s) => s.addSurface);
  const updateWall = useDesignerStore((s) => s.updateWall);
  // Floor moves carry the walls/glass/roofs resting on them; commitSlabPatch persists the runs
  // that rode along (server entities — otherwise the next refetch snaps the glass back).
  const { commitSlabPatch: updateSlab } = useSlabEntityActions();
  const updateSurface = useDesignerStore((s) => s.updateSurface);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const { createPanel, deletePanel, deleteRun, persistPanelHardware } = useDesignerEntityActions();
  // WHY not raw updateWall: the arc flip changes rotationDeg, and any wall pose change must
  // co-move + persist the glass attached to it or the panes are left behind.
  const { commitWallPatch } = useWallEntityActions();
  const { autofill } = useWallAutofill();
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  const cornerFillMode = useDesignerStore((s) => s.cornerFillMode);
  const setCornerFillMode = useDesignerStore((s) => s.setCornerFillMode);
  const applyScenePatch = useDesignerStore((s) => s.applyScenePatch);
  const { deleteMultiSelection } = useMultiSelectionDelete();

  const groupWalls = (groupId: string | null) => {
    // WHY the lock matters for a mere group id: membership widens the co-move set of every later
    // drag, stack and rotate, so grouping a locked wall would move it through the back door.
    const locked = lockedBodyIds(useDesignerStore.getState().scene);
    const { ids, blocked } = dropLockedIds(multiSelection.wallIds, locked);
    if (blocked) notifyLockedBlocked();
    if (ids.size === 0) return;
    applyScenePatch((s) => ({
      ...s,
      walls: (s.walls ?? []).map((w) => (ids.has(w.id) ? { ...w, groupId } : w)),
    }));
  };
  const { alignCenters, distributeEvenly, joinEndToEnd, equalizeHeights, equalizeLengths } =
    useMultiAlignActions();

  const roofOverSelection = () => {
    const selectedWalls = walls.filter((w) => multiSelection.wallIds.includes(w.id));
    const surface = computeRoofSurfaceOverWalls(selectedWalls);
    if (surface) {
      const id = crypto.randomUUID();
      addSurface({ ...surface, id });
      setSelection({
        kind: 'surface',
        runId: null,
        panelId: null,
        connectionId: null,
        hardwareId: null,
        wallId: null,
        slabId: null,
        surfaceId: id,
      });
      return;
    }
    const roof = computeRoofOverWalls(selectedWalls);
    if (!roof) {
      queueToast({
        dedupeKey: 'glass-roof-over-selection',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.MultiSelect.RoofNeedsWalls', {
          defaultValue: 'Çatı için en az üç duvar seçin.',
        }),
      });
      return;
    }
    const id = crypto.randomUUID();
    addSlab({ ...roof, id });
    setSelection({
      kind: 'slab',
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: id,
    });
  };

  const multiCount =
    multiSelection.runIds.length + multiSelection.wallIds.length + multiSelection.slabIds.length;

  if (multiCount > 0) {
    return (
      <div className="pointer-events-none flex min-w-[12rem] flex-1 justify-end">
        <div className="pointer-events-auto flex max-w-full flex-wrap items-center justify-end gap-1 rounded-lg border border-slate-200 bg-white/90 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/90">
          <span className="px-2 text-xs font-medium text-slate-600 dark:text-slate-300">
            {t('GlassEnclosure.Designer.MultiSelect.Count', {
              defaultValue: '{{count}} öğe seçili',
              count: multiCount,
            })}
          </span>
          <ToolbarButton
            icon={<AlignCenterVertical size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.AlignX', { defaultValue: 'X hizala' })}
            onClick={() => alignCenters('x')}
          />
          <ToolbarButton
            icon={<AlignCenterHorizontal size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.AlignY', { defaultValue: 'Y hizala' })}
            onClick={() => alignCenters('y')}
          />
          <ToolbarButton
            icon={<AlignHorizontalSpaceAround size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.DistributeX', {
              defaultValue: 'X eşit dağıt',
            })}
            onClick={() => distributeEvenly('x')}
          />
          <ToolbarButton
            icon={<AlignVerticalSpaceAround size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.DistributeY', {
              defaultValue: 'Y eşit dağıt',
            })}
            onClick={() => distributeEvenly('y')}
          />
          <ToolbarButton
            icon={<Link2 size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.Join', {
              defaultValue: 'Uç uca birleştir',
            })}
            onClick={() => joinEndToEnd()}
          />
          <ToolbarButton
            icon={<UnfoldVertical size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.EqualHeights', {
              defaultValue: 'Yükseklikleri eşitle',
            })}
            onClick={() => equalizeHeights()}
          />
          <ToolbarButton
            icon={<Ruler size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.EqualLengths', {
              defaultValue: 'Uzunlukları eşitle',
            })}
            onClick={() => equalizeLengths()}
          />
          <ToolbarButton
            icon={<Wand2 size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.FillGaps', {
              defaultValue: 'Araları camla doldur',
            })}
            onClick={() => void autofill()}
          />
          {multiSelection.wallIds.length >= 2 && (
            <>
              <ToolbarButton
                icon={<Spline size={13} />}
                label={`${t('GlassEnclosure.Designer.MultiSelect.CornerFill', {
                  defaultValue: 'Köşe dolgusu',
                })}: ${t(`GlassEnclosure.Designer.MultiSelect.CornerFillMode.${cornerFillMode}`, {
                  defaultValue: cornerFillMode,
                })}`}
                onClick={() =>
                  setCornerFillMode(
                    CORNER_FILL_ORDER[
                      (CORNER_FILL_ORDER.indexOf(cornerFillMode) + 1) % CORNER_FILL_ORDER.length
                    ],
                  )
                }
              />
              <ToolbarButton
                icon={<GroupIcon size={13} />}
                label={t('GlassEnclosure.Designer.MultiSelect.Group', {
                  defaultValue: 'Grupla (birlikte taşınır)',
                })}
                onClick={() => groupWalls(crypto.randomUUID())}
              />
              <ToolbarButton
                icon={<Ungroup size={13} />}
                label={t('GlassEnclosure.Designer.MultiSelect.Ungroup', {
                  defaultValue: 'Grubu çöz',
                })}
                onClick={() => groupWalls(null)}
              />
            </>
          )}
          {multiSelection.wallIds.length >= 3 && (
            <ToolbarButton
              icon={<Home size={13} />}
              label={t('GlassEnclosure.Designer.MultiSelect.RoofOverSelection', {
                defaultValue: 'Seçime çatı ekle',
              })}
              onClick={() => roofOverSelection()}
            />
          )}
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.DeleteAll', {
              defaultValue: 'Seçilenleri sil',
            })}
            danger
            onClick={() => deleteMultiSelection()}
          />
        </div>
      </div>
    );
  }

  if (!selection.kind) return null;

  const run = runs.find((r) => r.id === selection.runId);
  const clear = () =>
    setSelection({ kind: null, runId: null, panelId: null, connectionId: null, hardwareId: null });

  const selectRun = (runId: string) =>
    setSelection({ kind: 'run', runId, panelId: null, connectionId: null, hardwareId: null });

  const handleAddPanel = async () => {
    if (!run) return;
    const template = run.panels[run.panels.length - 1];
    const created = await createPanel(run.id, template, glassTypes[0]?.id ?? '');
    if (created) selectRun(run.id);
  };

  const lockToggle = (locked: boolean, onToggle: () => void): ReactNode => (
    <ToolbarButton
      icon={locked ? <Lock size={13} /> : <LockOpen size={13} />}
      label={
        locked
          ? t('GlassEnclosure.Designer.Unlock', { defaultValue: 'Kilidi aç' })
          : t('GlassEnclosure.Designer.Lock', { defaultValue: 'Kilitle' })
      }
      onClick={onToggle}
    />
  );

  const mirrorButton = (onMirror: () => void): ReactNode => (
    <ToolbarButton
      icon={<FlipHorizontal2 size={13} />}
      label={t('GlassEnclosure.Designer.Mirror', { defaultValue: 'Yatay aynala' })}
      onClick={onMirror}
    />
  );
  const arrayButton = (onArray: () => void): ReactNode => (
    <ToolbarButton
      icon={<Copy size={13} />}
      label={t('GlassEnclosure.Designer.Array', { defaultValue: 'Dizi (3×)' })}
      onClick={onArray}
    />
  );

  const solidObstaclesExcluding = (excludeId: string): PlanFootprint[] => {
    const s = useDesignerStore.getState().scene;
    return [
      ...(s.walls ?? [])
        .filter((w) => w.id !== excludeId)
        .map((w) => buildWallFootprint(w, 0, 0, w.rotationDeg)),
      ...s.runs
        .filter((r) => r.id !== excludeId)
        .map((r) => buildRunFootprint(r, 0, 0, r.rotationDeg)),
      ...(s.slabs ?? [])
        .filter((sl) => sl.id !== excludeId)
        .map((sl) => buildSlabFootprint(sl, 0, 0, sl.rotationDeg)),
    ];
  };

  const surfaceObstaclesExcluding = (excludeId: string): PlanFootprint[] => {
    const s = useDesignerStore.getState().scene;
    return (s.surfaces ?? [])
      .filter((su) => su.id !== excludeId)
      .map((su) => buildSurfaceFootprint(su));
  };

  const acceptArraySlots = (
    obstacles: PlanFootprint[],
    footprintAt: (k: number) => PlanFootprint,
  ): number[] => {
    const accepted: number[] = [];
    let skipped = 0;
    for (let k = 1; k < ARRAY_COUNT; k += 1) {
      const footprint = footprintAt(k);
      if (penetratesAny(footprint, obstacles)) {
        skipped += 1;
        continue;
      }
      obstacles.push({ ...footprint, ownerId: `${footprint.ownerId}#arr${k}` });
      accepted.push(k);
    }
    if (skipped > 0) {
      queueToast({
        dedupeKey: 'glass-array-overlap',
        variant: accepted.length > 0 ? 'warning' : 'error',
        description: t('GlassEnclosure.Designer.ArrayOverlap', {
          defaultValue: '{{placed}} kopya eklendi, {{skipped}} tanesi çakışma nedeniyle atlandı.',
          placed: accepted.length,
          skipped,
        }),
      });
    }
    return accepted;
  };

  const renderActions = (): ReactNode => {
    if (selection.kind === 'run' && run) {
      return (
        <>
          <ToolbarButton
            icon={<Plus size={13} />}
            label={t('GlassEnclosure.Designer.AddPanel', { defaultValue: 'Add panel' })}
            onClick={() => void handleAddPanel()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.DeleteRun', { defaultValue: 'Delete run' })}
            danger
            onClick={() => {
              void deleteRun(run.id);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'panel' && run) {
      const panel = run.panels.find((p) => p.id === selection.panelId);
      return (
        <>
          <ToolbarButton
            icon={<Plus size={13} />}
            label={t('GlassEnclosure.Designer.AddPanel', { defaultValue: 'Add panel' })}
            onClick={() => void handleAddPanel()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.DeletePanel', { defaultValue: 'Delete panel' })}
            danger
            onClick={() => {
              if (!panel) return;
              void deletePanel(run.id, panel.id);
              selectRun(run.id);
            }}
          />
        </>
      );
    }

    if (selection.kind === 'hardware' && run) {
      const panel = run.panels.find((p) => p.id === selection.panelId);
      const item = panel?.hardware.find((h) => h.id === selection.hardwareId);
      return (
        <>
          <ToolbarButton
            icon={<Copy size={13} />}
            label={t('Common.Duplicate', { defaultValue: 'Duplicate' })}
            onClick={() => {
              if (!panel || !item) return;
              const clone = { ...item, id: crypto.randomUUID(), offsetXmm: item.offsetXmm + 30 };
              addHardware(run.id, panel.id, clone);
              void persistPanelHardware(run.id, panel.id);
              setSelection({
                kind: 'hardware',
                runId: run.id,
                panelId: panel.id,
                connectionId: null,
                hardwareId: clone.id,
              });
            }}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('Common.Delete', { defaultValue: 'Delete' })}
            danger
            onClick={() => {
              if (!panel || !item) return;
              removeHardware(run.id, panel.id, item.id);
              setSelection({
                kind: 'panel',
                runId: run.id,
                panelId: panel.id,
                connectionId: null,
                hardwareId: null,
              });
            }}
          />
        </>
      );
    }

    if (selection.kind === 'wall' && selection.wallId) {
      const wallObj = walls.find((w) => w.id === selection.wallId);
      return (
        <>
          {lockToggle(Boolean(wallObj?.locked), () =>
            updateWall(selection.wallId as string, { locked: !wallObj?.locked }),
          )}
          {wallObj && mirrorButton(() => commitWallPatch(wallObj, mirrorWallPatch(wallObj)))}
          {wallObj &&
            arrayButton(() => {
              // WHY the chord and not rotationDeg: on an arc body rotationDeg is the start
              // tangent, so stepping along it walks the copies off at an angle (a diagonal
              // staircase) instead of laying them end to end along the body axis.
              const chord = bodyChordVectorMm(wallObj);
              const span = Math.hypot(chord.xMm, chord.yMm) || wallObj.lengthMm;
              const dx = chord.xMm / span;
              const dy = chord.yMm / span;
              const step = span + ARRAY_GAP_MM;
              const offsetAt = (k: number) => ({
                offX: Math.round(wallObj.originX + dx * step * k) - wallObj.originX,
                offY: Math.round(wallObj.originY + dy * step * k) - wallObj.originY,
              });
              const cloneAt = (k: number) => {
                const { offX, offY } = offsetAt(k);
                return {
                  ...structuredClone(wallObj),
                  id: crypto.randomUUID(),
                  groupId: null,
                  originX: wallObj.originX + offX,
                  originY: wallObj.originY + offY,
                  openings: (wallObj.openings ?? []).map((o) => ({
                    ...o,
                    id: crypto.randomUUID(),
                  })),
                  features: (wallObj.features ?? []).map((f) => ({
                    ...f,
                    id: crypto.randomUUID(),
                  })),
                };
              };
              const accepted = acceptArraySlots(solidObstaclesExcluding(wallObj.id), (k) => {
                const { offX, offY } = offsetAt(k);
                return buildWallFootprint(wallObj, offX, offY, wallObj.rotationDeg);
              });
              if (accepted.length > 0)
                applyScenePatch((s) => ({
                  ...s,
                  walls: [...(s.walls ?? []), ...accepted.map(cloneAt)],
                }));
            })}
          <ToolbarButton
            icon={<Wand2 size={13} />}
            label={t('GlassEnclosure.Designer.Wall.Autofill', {
              defaultValue: 'Boşlukları camla doldur',
            })}
            onClick={() => void autofill()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.Wall.Delete', { defaultValue: 'Duvarı sil' })}
            danger
            onClick={() => {
              removeWall(selection.wallId as string);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'wallFeature' && selection.wallId && selection.featureId) {
      return (
        <ToolbarButton
          icon={<Trash2 size={13} />}
          label={t('GlassEnclosure.Designer.WallFeature.Remove', { defaultValue: 'Katmanı sil' })}
          danger
          onClick={() => {
            removeWallFeature(selection.wallId as string, selection.featureId as string);
            setSelection({
              kind: 'wall',
              runId: null,
              panelId: null,
              connectionId: null,
              hardwareId: null,
              wallId: selection.wallId,
            });
          }}
        />
      );
    }

    if (selection.kind === 'slabFeature' && selection.slabId && selection.featureId) {
      return (
        <ToolbarButton
          icon={<Trash2 size={13} />}
          label={t('GlassEnclosure.Designer.WallFeature.Remove', { defaultValue: 'Katmanı sil' })}
          danger
          onClick={() => {
            removeSlabFeature(selection.slabId as string, selection.featureId as string);
            setSelection({
              kind: 'slab',
              runId: null,
              panelId: null,
              connectionId: null,
              hardwareId: null,
              wallId: null,
              slabId: selection.slabId,
            });
          }}
        />
      );
    }

    if (selection.kind === 'slab' && selection.slabId) {
      const slab = slabs.find((item) => item.id === selection.slabId);
      return (
        <>
          {lockToggle(Boolean(slab?.locked), () =>
            updateSlab(selection.slabId as string, { locked: !slab?.locked }),
          )}
          {slab && mirrorButton(() => updateSlab(slab.id, mirrorSlabPatch(slab)))}
          {slab &&
            arrayButton(() => {
              const chord = bodyChordVectorMm(slab);
              const span = Math.hypot(chord.xMm, chord.yMm) || slab.lengthMm;
              const dx = chord.xMm / span;
              const dy = chord.yMm / span;
              const step = span + ARRAY_GAP_MM;
              const offsetAt = (k: number) => ({
                offX: Math.round(slab.originX + dx * step * k) - slab.originX,
                offY: Math.round(slab.originY + dy * step * k) - slab.originY,
              });
              const cloneAt = (k: number) => {
                const { offX, offY } = offsetAt(k);
                return {
                  ...structuredClone(slab),
                  id: crypto.randomUUID(),
                  originX: slab.originX + offX,
                  originY: slab.originY + offY,
                  features: (slab.features ?? []).map((f) => ({ ...f, id: crypto.randomUUID() })),
                };
              };
              const accepted = acceptArraySlots(solidObstaclesExcluding(slab.id), (k) => {
                const { offX, offY } = offsetAt(k);
                return buildSlabFootprint(slab, offX, offY, slab.rotationDeg);
              });
              if (accepted.length > 0)
                applyScenePatch((s) => ({
                  ...s,
                  slabs: [...(s.slabs ?? []), ...accepted.map(cloneAt)],
                }));
            })}
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={
              slab?.kind === 'roof'
                ? t('GlassEnclosure.Designer.Slab.DeleteRoof', { defaultValue: 'Çatıyı sil' })
                : t('GlassEnclosure.Designer.Slab.DeleteFloor', { defaultValue: 'Zemini sil' })
            }
            danger
            onClick={() => {
              removeSlab(selection.slabId as string);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'surface' && selection.surfaceId) {
      const surfaceObj = surfaces.find((s) => s.id === selection.surfaceId);
      return (
        <>
          {lockToggle(Boolean(surfaceObj?.locked), () =>
            updateSurface(selection.surfaceId as string, { locked: !surfaceObj?.locked }),
          )}
          {surfaceObj &&
            surfaceObj.points.length > 0 &&
            mirrorButton(() => updateSurface(surfaceObj.id, mirrorSurfacePatch(surfaceObj)))}
          {surfaceObj &&
            surfaceObj.points.length > 0 &&
            arrayButton(() => {
              const xs = surfaceObj.points.map((p) => p.x);
              const step = Math.max(...xs) - Math.min(...xs) + ARRAY_GAP_MM;
              const pointsAt = (k: number) =>
                surfaceObj.points.map((p) => ({ x: p.x + step * k, y: p.y }));
              const cloneAt = (k: number) => ({
                ...structuredClone(surfaceObj),
                id: crypto.randomUUID(),
                points: pointsAt(k),
              });
              const accepted = acceptArraySlots(surfaceObstaclesExcluding(surfaceObj.id), (k) =>
                buildSurfaceFootprint({ ...surfaceObj, points: pointsAt(k) }),
              );
              if (accepted.length > 0)
                applyScenePatch((s) => ({
                  ...s,
                  surfaces: [...(s.surfaces ?? []), ...accepted.map(cloneAt)],
                }));
            })}
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.Surface.Delete', { defaultValue: 'Yüzeyi sil' })}
            danger
            onClick={() => {
              removeSurface(selection.surfaceId as string);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'connection' && selection.connectionId) {
      return (
        <ToolbarButton
          icon={<Trash2 size={13} />}
          label={t('Common.Delete', { defaultValue: 'Delete' })}
          danger
          onClick={() => {
            removeConnection(selection.connectionId as string);
            clear();
          }}
        />
      );
    }

    return null;
  };

  const actions = renderActions();
  if (!actions) return null;

  return (
    <div className="pointer-events-none flex min-w-[12rem] flex-1 justify-end">
      <div className="pointer-events-auto flex max-w-full flex-wrap items-center justify-end gap-1 rounded-lg border border-slate-200 bg-white/90 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/90">
        {actions}
      </div>
    </div>
  );
}

interface ToolbarButtonProps {
  icon: ReactNode;
  label: string;
  onClick: () => void;
  danger?: boolean;
}

const ToolbarButton = ({ icon, label, onClick, danger }: ToolbarButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    title={label}
    aria-label={label}
    className={cn(
      'inline-flex h-7 items-center gap-1 rounded-md px-2 text-xs font-medium transition',
      danger
        ? 'text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30'
        : 'text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800',
    )}
  >
    {icon}
    <span className="hidden md:inline">{label}</span>
  </button>
);
