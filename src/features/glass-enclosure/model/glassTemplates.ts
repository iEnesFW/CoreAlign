import type { SceneSlabState, SceneState, SceneWallState } from './project.types';
import type { TemplateRunDraft } from './templates';

export interface UserGlassTemplate {
  id: string;
  name: string;
  walls: SceneWallState[];
  slabs: SceneSlabState[];
  runs: TemplateRunDraft[];
}

const originMin = (scene: SceneState): { minX: number; minY: number } => {
  let minX = Infinity;
  let minY = Infinity;
  const consider = (x: number, y: number) => {
    if (x < minX) minX = x;
    if (y < minY) minY = y;
  };
  for (const wall of scene.walls ?? []) consider(wall.originX, wall.originY);
  for (const slab of scene.slabs ?? []) consider(slab.originX, slab.originY);
  for (const run of scene.runs) consider(run.originX, run.originY);
  return { minX: Number.isFinite(minX) ? minX : 0, minY: Number.isFinite(minY) ? minY : 0 };
};

// Snapshot the scene's structure anchored at (0,0) so it can be dropped into any project later. The
// group bond is scene-specific → cleared; ids ride along but the insert path re-generates them.
export const captureSceneAsTemplate = (
  scene: SceneState,
  id: string,
  name: string,
): UserGlassTemplate => {
  const { minX, minY } = originMin(scene);
  const shift = <T extends { originX: number; originY: number }>(o: T): T => ({
    ...o,
    originX: Math.round(o.originX - minX),
    originY: Math.round(o.originY - minY),
  });
  return {
    id,
    name,
    walls: (scene.walls ?? []).map((wall) => ({ ...shift(wall), groupId: null })),
    slabs: (scene.slabs ?? []).map(shift),
    runs: scene.runs.map((run) => ({
      originX: Math.round(run.originX - minX),
      originY: Math.round(run.originY - minY),
      rotationDeg: run.rotationDeg,
      lengthMm: run.lengthMm,
      heightMm: run.heightMm,
      geomArcRadiusMm: run.geomArcRadiusMm ?? undefined,
      geomArcSweepDeg: run.geomArcSweepDeg ?? undefined,
      arcGlassBent: run.arcGlassBent ?? undefined,
    })),
  };
};

export const isTemplateEmpty = (template: UserGlassTemplate): boolean =>
  template.walls.length === 0 && template.slabs.length === 0 && template.runs.length === 0;

// Loose validation for the localStorage payload — drop anything that isn't a well-formed list of
// named templates carrying arrays (a schema bump or hand-edit must never crash the designer).
export const parseUserGlassTemplates = (raw: unknown): UserGlassTemplate[] | null => {
  if (!Array.isArray(raw)) return null;
  const out: UserGlassTemplate[] = [];
  for (const item of raw) {
    if (!item || typeof item !== 'object') continue;
    const t = item as Partial<UserGlassTemplate>;
    if (typeof t.id !== 'string' || typeof t.name !== 'string') continue;
    if (!Array.isArray(t.walls) || !Array.isArray(t.slabs) || !Array.isArray(t.runs)) continue;
    out.push({ id: t.id, name: t.name, walls: t.walls, slabs: t.slabs, runs: t.runs });
  }
  return out;
};

export interface GlassTemplatePayload {
  walls: SceneWallState[];
  slabs: SceneSlabState[];
  runs: TemplateRunDraft[];
}

export const templatePayloadJson = (template: UserGlassTemplate): string =>
  JSON.stringify({ walls: template.walls, slabs: template.slabs, runs: template.runs });

export const parseTemplatePayload = (payloadJson: string): GlassTemplatePayload | null => {
  let raw: unknown;
  try {
    raw = JSON.parse(payloadJson);
  } catch {
    return null;
  }
  if (!raw || typeof raw !== 'object') return null;
  const p = raw as Partial<GlassTemplatePayload>;
  if (!Array.isArray(p.walls) || !Array.isArray(p.slabs) || !Array.isArray(p.runs)) return null;
  return { walls: p.walls, slabs: p.slabs, runs: p.runs };
};
