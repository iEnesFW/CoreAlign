import { supportTopBelowMm } from '@/shared/three-engine';
import {
  buildRunFootprint,
  buildSlabFootprint,
  buildSurfaceFootprint,
  buildWallFootprint,
} from '../scene/interaction/planCollision';
import type { PlanFootprint } from '@/shared/three-engine';
import type { SceneState } from './project.types';

/**
 * Drop every body that lost its support onto the next thing below it (or the ground).
 *
 * WHY this exists: an object's elevation used to be resolved ONLY inside a drag preview. Delete the
 * wall a roof stood on, or shrink the slab under a run, and the orphan just hung in the air until
 * somebody happened to drag it. Nothing re-settled the scene.
 *
 * Roofs are deliberately exempt — a canopy or pergola legitimately floats over an open span.
 */

export const isGravityExempt = (kind: string) => kind === 'roof';

interface Settleable {
  id: string;
  baseMm: number;
  footprint: PlanFootprint;
  // A body that does not fall (a roof, or one the user locked) is still a SUPPORT — it stays in the
  // list so nothing drops through it, it is only skipped when we decide what to move.
  falls: boolean;
  apply: (topMm: number) => void;
}

export const settleScene = (scene: SceneState): SceneState => {
  const next: {
    walls: Record<string, number>;
    runs: Record<string, number>;
    slabs: Record<string, number>;
    surfaces: Record<string, number>;
  } = { walls: {}, runs: {}, slabs: {}, surfaces: {} };

  const items: Settleable[] = [];
  for (const wall of scene.walls ?? []) {
    const baseMm = wall.geomZ ?? 0;
    items.push({
      id: wall.id,
      baseMm,
      footprint: buildWallFootprint(wall, 0, 0, wall.rotationDeg),
      falls: !wall.locked,
      apply: (topMm) => {
        next.walls[wall.id] = topMm;
      },
    });
  }
  for (const run of scene.runs) {
    const baseMm = run.geomZ ?? 0;
    items.push({
      id: run.id,
      baseMm,
      footprint: buildRunFootprint(run, 0, 0, run.rotationDeg),
      falls: !run.locked,
      apply: (topMm) => {
        next.runs[run.id] = topMm;
      },
    });
  }
  for (const slab of scene.slabs ?? []) {
    items.push({
      id: slab.id,
      baseMm: slab.elevationMm,
      footprint: buildSlabFootprint(slab, 0, 0, slab.rotationDeg),
      falls: !isGravityExempt(slab.kind) && !slab.locked,
      apply: (topMm) => {
        next.slabs[slab.id] = topMm;
      },
    });
  }
  for (const surface of scene.surfaces ?? []) {
    items.push({
      id: surface.id,
      baseMm: surface.elevationMm,
      footprint: buildSurfaceFootprint(surface, 0, 0),
      falls: !isGravityExempt(surface.kind) && !surface.locked,
      apply: (topMm) => {
        next.surfaces[surface.id] = topMm;
      },
    });
  }

  // Lowest first: settling a body changes what sits on top of it, and an ascending pass means the
  // thing underneath has already found its final height before anything reads it.
  items.sort((a, b) => a.baseMm - b.baseMm);

  // Supports are re-derived from the settled heights as we go, so a stack collapses in one pass.
  const settledTop = new Map<string, number>();
  const supportsFor = (self: Settleable): PlanFootprint[] =>
    items
      .filter((o) => o.id !== self.id)
      .map((o) => {
        const top = settledTop.get(o.id);
        if (top === undefined) return o.footprint;
        const shift = top - o.baseMm;
        return {
          ...o.footprint,
          zMinMm: o.footprint.zMinMm + shift,
          zMaxMm: o.footprint.zMaxMm + shift,
        };
      });

  for (const item of items) {
    if (!item.falls) continue;
    const top = supportTopBelowMm(item.footprint, supportsFor(item), item.baseMm);
    settledTop.set(item.id, top);
    // WHY only DOWNWARD: gravity makes things fall, it never lifts. A floor slab is authored at
    // -150 so that its TOP sits flush with the ground, and the ground fallback is 0 — so a plain
    // "top !== base" would RAISE it to 0. Its z-range then becomes [0,150], which overlaps every
    // wall standing at 0, and the collision solver starts refusing to drag anything onto the
    // floor. Lifting a body is the drag/Alt-stack path's job, never this one.
    if (top < item.baseMm) item.apply(top);
  }

  const changed =
    Object.keys(next.walls).length +
      Object.keys(next.runs).length +
      Object.keys(next.slabs).length +
      Object.keys(next.surfaces).length >
    0;
  if (!changed) return scene;

  return {
    ...scene,
    walls: (scene.walls ?? []).map((w) =>
      next.walls[w.id] !== undefined ? { ...w, geomZ: next.walls[w.id] } : w,
    ),
    runs: scene.runs.map((r) =>
      next.runs[r.id] !== undefined ? { ...r, geomZ: next.runs[r.id] } : r,
    ),
    slabs: (scene.slabs ?? []).map((s) =>
      next.slabs[s.id] !== undefined ? { ...s, elevationMm: next.slabs[s.id] } : s,
    ),
    surfaces: (scene.surfaces ?? []).map((s) =>
      next.surfaces[s.id] !== undefined ? { ...s, elevationMm: next.surfaces[s.id] } : s,
    ),
  };
};
