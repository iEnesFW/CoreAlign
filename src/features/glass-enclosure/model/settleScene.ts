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

const isExemptSlab = (kind: string) => kind === 'roof';

interface Settleable {
  id: string;
  baseMm: number;
  footprint: PlanFootprint;
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
      apply: (topMm) => {
        next.runs[run.id] = topMm;
      },
    });
  }
  for (const slab of scene.slabs ?? []) {
    if (isExemptSlab(slab.kind)) continue;
    items.push({
      id: slab.id,
      baseMm: slab.elevationMm,
      footprint: buildSlabFootprint(slab, 0, 0, slab.rotationDeg),
      apply: (topMm) => {
        next.slabs[slab.id] = topMm;
      },
    });
  }
  for (const surface of scene.surfaces ?? []) {
    if (isExemptSlab(surface.kind)) continue;
    items.push({
      id: surface.id,
      baseMm: surface.elevationMm,
      footprint: buildSurfaceFootprint(surface, 0, 0),
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
      })
      // A roof does not hold anything up, and neither does a body we have not settled yet if it
      // sits above us — supportTopBelowMm filters that, this just keeps the list honest.
      .concat(roofFootprints(scene));

  for (const item of items) {
    const top = supportTopBelowMm(item.footprint, supportsFor(item), item.baseMm);
    settledTop.set(item.id, top);
    if (top !== item.baseMm) item.apply(top);
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

// Roofs are exempt from settling but still HOLD THINGS UP — something placed on a canopy must not
// fall through it.
const roofFootprints = (scene: SceneState): PlanFootprint[] => [
  ...(scene.slabs ?? [])
    .filter((s) => isExemptSlab(s.kind))
    .map((s) => buildSlabFootprint(s, 0, 0, s.rotationDeg)),
  ...(scene.surfaces ?? [])
    .filter((s) => isExemptSlab(s.kind))
    .map((s) => buildSurfaceFootprint(s, 0, 0)),
];
