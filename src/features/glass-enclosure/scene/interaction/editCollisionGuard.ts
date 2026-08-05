import { queueToast } from '@/shared/api/toastQueue';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildSurfaceFootprint,
  buildWallFootprint,
  footprintsPenetrate,
  penetratesAny,
} from './planCollision';
import { useDesignerStore } from '../../model/designerStore';
import type { PlanFootprint } from './planCollision';

/**
 * The collision gate every NUMERIC / ACTION editor shares.
 *
 * WHY it is one writer: the 3D gizmos clamp, but the editors that write the same fields did not,
 * and they diverged body by body — the transform toolbar guarded a run's six geometry fields while
 * the run inspector wrote them raw, and the mirror buttons committed with no check at all next to an
 * array button that checks every copy. Same screen, same fields, opposite behaviour.
 *
 * `alreadyColliding` escape: a body that ALREADY overlaps something must stay editable, or a user
 * whose scene arrived overlapping (an old project, a stretch/rotate path) can never fix it. Only
 * obstacles that are currently CLEAR can veto — the same rule the wall/slab inspectors use.
 */
export const solidObstaclesExcept = (excludeIds: Set<string>): PlanFootprint[] => {
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
    // WHY roof surfaces only: a FLOOR surface spans grade with a z-range that touches every wall
    // standing on it, so including those would veto ordinary edits. A roof is a real body overhead.
    ...(s.surfaces ?? [])
      .filter((sf) => sf.kind === 'roof' && !excludeIds.has(sf.id))
      .map((sf) => buildSurfaceFootprint(sf, 0, 0)),
  ];
};

export const transformAllowed = (
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
