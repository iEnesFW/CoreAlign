import type { MultiSelection } from '../../model/designerStore';
import type { SceneState } from '../../model/project.types';
import type { AttachedRunSnapshot } from './attachedRunPreview';

const MM = 1000;

export interface MultiMoveMember {
  kind: 'run' | 'wall' | 'slab';
  id: string;
}

export const multiSelectionHas = (
  multi: MultiSelection,
  kind: MultiMoveMember['kind'],
  id: string,
): boolean => {
  if (kind === 'run') return multi.runIds.includes(id);
  if (kind === 'wall') return multi.wallIds.includes(id);
  return multi.slabIds.includes(id);
};

export const multiSelectionCount = (multi: MultiSelection): number =>
  multi.runIds.length + multi.wallIds.length + multi.slabIds.length;

export const captureMultiSnapshots = (
  scene: SceneState,
  multi: MultiSelection,
  exclude: MultiMoveMember,
): AttachedRunSnapshot[] => {
  const snapshots: AttachedRunSnapshot[] = [];
  for (const run of scene.runs) {
    if (exclude.kind === 'run' && exclude.id === run.id) continue;
    if (!multi.runIds.includes(run.id)) continue;
    snapshots.push({
      runId: run.id,
      originXMm: run.originX,
      originYMm: run.originY,
      rotationDeg: run.rotationDeg,
      baseYM: (run.geomZ ?? 0) / MM,
    });
  }
  for (const wall of scene.walls ?? []) {
    if (exclude.kind === 'wall' && exclude.id === wall.id) continue;
    if (!multi.wallIds.includes(wall.id)) continue;
    snapshots.push({
      runId: wall.id,
      originXMm: wall.originX,
      originYMm: wall.originY,
      rotationDeg: wall.rotationDeg,
      baseYM: 0,
    });
  }
  for (const slab of scene.slabs ?? []) {
    if (exclude.kind === 'slab' && exclude.id === slab.id) continue;
    if (!multi.slabIds.includes(slab.id)) continue;
    snapshots.push({
      runId: slab.id,
      originXMm: slab.originX,
      originYMm: slab.originY,
      rotationDeg: slab.rotationDeg,
      baseYM: slab.elevationMm / MM,
    });
  }
  return snapshots;
};
