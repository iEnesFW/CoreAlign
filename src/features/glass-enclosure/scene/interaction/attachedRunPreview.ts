import { getSceneRef } from './sceneRefs';
import { rotatePlanPointDeg } from './planTransform';
import type { SceneRunState } from '../../model/project.types';

export interface AttachedRunSnapshot {
  runId: string;
  originXMm: number;
  originYMm: number;
  rotationDeg: number;
  baseYM: number;
}

const MM = 1000;
const DEG2RAD = Math.PI / 180;

export const captureRunSnapshots = (
  runs: SceneRunState[],
  runIds: string[],
): AttachedRunSnapshot[] => {
  const wanted = new Set(runIds);
  return runs
    .filter((run) => wanted.has(run.id))
    .map((run) => ({
      runId: run.id,
      originXMm: run.originX,
      originYMm: run.originY,
      rotationDeg: run.rotationDeg,
      baseYM: (run.geomZ ?? 0) / MM,
    }));
};

export const previewSnapshotsMove = (
  snapshots: AttachedRunSnapshot[],
  dxMm: number,
  dyMm: number,
) => {
  for (const snap of snapshots) {
    const group = getSceneRef(snap.runId);
    if (!group) continue;
    group.position.set((snap.originXMm + dxMm) / MM, snap.baseYM, (snap.originYMm + dyMm) / MM);
    group.rotation.y = -snap.rotationDeg * DEG2RAD;
  }
};

export const previewSnapshotsRotation = (
  snapshots: AttachedRunSnapshot[],
  pivotXMm: number,
  pivotYMm: number,
  sweepDeg: number,
) => {
  for (const snap of snapshots) {
    const group = getSceneRef(snap.runId);
    if (!group) continue;
    const origin = rotatePlanPointDeg(snap.originXMm, snap.originYMm, pivotXMm, pivotYMm, sweepDeg);
    group.position.set(origin.x / MM, snap.baseYM, origin.y / MM);
    group.rotation.y = -(snap.rotationDeg + sweepDeg) * DEG2RAD;
  }
};
