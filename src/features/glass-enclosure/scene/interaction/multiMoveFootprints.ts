import { buildRunFootprint, buildSlabFootprint, buildWallFootprint } from './planCollision';
import { multiSelectionHas } from './multiMove';
import type { MultiMoveMember } from './multiMove';
import type { PlanFootprint } from './planCollision';
import type { MultiSelection } from '../../model/designerStore';
import type {
  SceneRunState,
  SceneSlabState,
  SceneState,
  SceneWallState,
} from '../../model/project.types';

/**
 * The bodies that travel WITH the dragged one, as real bodies rather than just ids.
 *
 * WHY this exists: every builder removes the co-moving multi-selection members from its OBSTACLE
 * list — correct, they are not obstacles to themselves — but only WallObject also added them to the
 * MOVING set that the collision clamp measures. In RunGroup, ArcRunGroup and SlabObject the clamp
 * therefore never saw them: grab a group by the glass run and the wall travelling with it slid
 * straight into an unselected wall and committed there, because `commitGroupMove` re-checks nothing.
 * Exclusion and motion have to be symmetric, and the only way three copies stay symmetric is one
 * writer.
 */
export interface MultiMoveBodies {
  walls: SceneWallState[];
  runs: SceneRunState[];
  slabs: SceneSlabState[];
}

export const EMPTY_MULTI_BODIES: MultiMoveBodies = { walls: [], runs: [], slabs: [] };

/**
 * Snapshot the co-movers at gesture start. `alreadyMovingRunIds` is for the wall case, whose bonded
 * glass is carried by a separate list — counting it twice would double the footprint, not the move.
 */
export const captureMultiBodies = (
  scene: SceneState,
  multi: MultiSelection,
  self: MultiMoveMember,
  alreadyMovingRunIds: ReadonlySet<string> = new Set<string>(),
): MultiMoveBodies => {
  if (!multiSelectionHas(multi, self.kind, self.id)) return EMPTY_MULTI_BODIES;
  return {
    walls: (scene.walls ?? []).filter(
      (w) => multi.wallIds.includes(w.id) && !(self.kind === 'wall' && w.id === self.id),
    ),
    runs: scene.runs.filter(
      (r) =>
        multi.runIds.includes(r.id) &&
        !alreadyMovingRunIds.has(r.id) &&
        !(self.kind === 'run' && r.id === self.id),
    ),
    slabs: (scene.slabs ?? []).filter(
      (b) => multi.slabIds.includes(b.id) && !(self.kind === 'slab' && b.id === self.id),
    ),
  };
};

/** The co-movers' footprints at the drag offset, to append to the dragged body's own. */
export const multiBodyFootprints = (
  bodies: MultiMoveBodies,
  dxMm: number,
  dyMm: number,
): PlanFootprint[] => [
  ...bodies.walls.map((w) => buildWallFootprint(w, dxMm, dyMm, w.rotationDeg)),
  ...bodies.runs.map((r) => buildRunFootprint(r, dxMm, dyMm, r.rotationDeg)),
  ...bodies.slabs.map((b) => buildSlabFootprint(b, dxMm, dyMm, b.rotationDeg)),
];
