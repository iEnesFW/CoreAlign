import type { SceneState } from './project.types';

export interface WallStackInput {
  wallId: string;
  dxMm: number;
  dyMm: number;
  targetZMm: number;
  groupWallIds: string[];
  attachedRunIds: string[];
}

export const applyWallStack = (scene: SceneState, input: WallStackInput): SceneState => {
  const walls = scene.walls ?? [];
  const wall = walls.find((w) => w.id === input.wallId);
  if (!wall) return scene;

  // WHY: the stacked wall lands ON the support (absolute Z), but everything riding it — group
  // siblings and attached glass — keeps its own offset relative to the wall, so they travel by the
  // DELTA. Writing the absolute Z to them would flatten a run mounted above the wall base.
  const deltaZMm = input.targetZMm - (wall.geomZ ?? 0);
  const groupWallIds = new Set(input.groupWallIds);
  const attachedRunIds = new Set(input.attachedRunIds);

  return {
    ...scene,
    walls: walls.map((w) => {
      if (w.id === input.wallId) {
        return {
          ...w,
          originX: Math.round(w.originX + input.dxMm),
          originY: Math.round(w.originY + input.dyMm),
          geomZ: input.targetZMm,
        };
      }
      if (!groupWallIds.has(w.id)) return w;
      return {
        ...w,
        originX: Math.round(w.originX + input.dxMm),
        originY: Math.round(w.originY + input.dyMm),
        geomZ: (w.geomZ ?? 0) + deltaZMm,
      };
    }),
    runs: scene.runs.map((r) =>
      attachedRunIds.has(r.id)
        ? {
            ...r,
            originX: Math.round(r.originX + input.dxMm),
            originY: Math.round(r.originY + input.dyMm),
            geomZ: (r.geomZ ?? 0) + deltaZMm,
          }
        : r,
    ),
  };
};
