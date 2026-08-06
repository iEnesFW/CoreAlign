import { deriveArcFromSweep } from './arcGeometry';
import { rotationForChord } from '../geometry/curvature';
import { slabArcDefaultSweepSign } from '../scene/builders/curvedSlabGeometry';
import type { SceneSlabState, SceneWallState } from './project.types';

export type GlassTemplateKey =
  | 'l-walls'
  | 'u-walls'
  | 'room'
  | 'gable-roof'
  | 'barrel-roof'
  | 'arc-roof'
  | 'arc-run';

export interface TemplateRunDraft {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  heightMm: number;
  geomArcRadiusMm?: number;
  geomArcSweepDeg?: number;
  arcGlassBent?: boolean;
}

export interface GlassTemplate {
  key: GlassTemplateKey;
  walls: Omit<SceneWallState, 'id'>[];
  slabs: Omit<SceneSlabState, 'id'>[];
  runs: TemplateRunDraft[];
}

export interface TemplateParams {
  widthMm: number;
  depthMm: number;
  heightMm: number;
}

export const DEFAULT_TEMPLATE_PARAMS: TemplateParams = {
  widthMm: 3000,
  depthMm: 2000,
  heightMm: 2600,
};

const WALL_THICKNESS_MM = 200;
const SLAB_THICKNESS_MM = 150;

const wall = (
  originX: number,
  originY: number,
  rotationDeg: number,
  lengthMm: number,
  heightMm: number,
  openings: SceneWallState['openings'] = [],
): Omit<SceneWallState, 'id'> => ({
  originX,
  originY,
  rotationDeg,
  lengthMm,
  heightMm,
  heightEndMm: null,
  thicknessMm: WALL_THICKNESS_MM,
  colorHex: null,
  geomZ: 0,
  openings,
  features: [],
});

const roof = (
  lengthMm: number,
  depthMm: number,
  elevationMm: number,
  extra: Partial<SceneSlabState> = {},
): Omit<SceneSlabState, 'id'> => ({
  kind: 'roof',
  originX: 0,
  originY: 0,
  rotationDeg: 0,
  lengthMm,
  depthMm,
  thicknessMm: SLAB_THICKNESS_MM,
  elevationMm,
  colorHex: null,
  features: [],
  ...extra,
});

// One-click compositions anchored at (0,0); the insertion hook translates every piece to the
// scene's drop point. Walls/slabs are scene-blob entities; runs are server entities the hook
// creates through the same mutation path autofill uses.
export const buildGlassTemplate = (
  key: GlassTemplateKey,
  params: TemplateParams = DEFAULT_TEMPLATE_PARAMS,
): GlassTemplate => {
  const { widthMm: W, depthMm: D, heightMm: H } = params;
  switch (key) {
    case 'l-walls':
      return { key, slabs: [], runs: [], walls: [wall(0, 0, 0, W, H), wall(W, 0, 90, D, H)] };
    case 'u-walls':
      return {
        key,
        slabs: [],
        runs: [],
        walls: [wall(0, 0, 90, D, H), wall(0, D, 0, W, H), wall(W, D, -90, D, H)],
      };
    case 'room':
      return {
        key,
        slabs: [],
        runs: [],
        walls: [
          wall(0, 0, 0, W, H),
          wall(0, 0, 90, D, H),
          wall(0, D, 0, W, H),
          wall(W, 0, 90, D, H),
        ],
      };
    case 'gable-roof':
      return {
        key,
        walls: [],
        runs: [],
        slabs: [
          roof(W, D, H, {
            pitchRiseMm: Math.max(300, Math.round(D * 0.3)),
            pitchType: 'symmetric',
          }),
        ],
      };
    case 'barrel-roof':
      return {
        key,
        walls: [],
        runs: [],
        slabs: [roof(W, D, H, { arcRiseMm: Math.max(250, Math.round(D * 0.25)) })],
      };
    case 'arc-roof': {
      const sign = slabArcDefaultSweepSign('length');
      const derived = deriveArcFromSweep(W, 90);
      return {
        key,
        walls: [],
        runs: [],
        slabs: [
          roof(W, D, H, {
            geomArcRadiusMm: derived.radiusMm,
            geomArcSweepDeg: sign * Math.abs(derived.sweepDeg),
            slabArcAxis: 'length',
          }),
        ],
      };
    }
    case 'arc-run': {
      // Chord along +x, bulge toward +y (canonical: bulge opposite the sweep sign → negative
      // sweep). rotationForChord supplies the ROLLED start tangent so both ends land on the chord —
      // the same rule every arc edit uses, rather than a hand-computed constant.
      const derived = deriveArcFromSweep(W, 90);
      const sweepDeg = -Math.abs(derived.sweepDeg);
      return {
        key,
        walls: [],
        slabs: [],
        runs: [
          {
            originX: 0,
            originY: 0,
            rotationDeg: rotationForChord(0, sweepDeg),
            lengthMm: derived.chordMm,
            heightMm: H,
            geomArcRadiusMm: derived.radiusMm,
            geomArcSweepDeg: sweepDeg,
            arcGlassBent: true,
          },
        ],
      };
    }
  }
};

export interface TemplatePlanBounds {
  minXMm: number;
  maxXMm: number;
  minYMm: number;
  maxYMm: number;
  zMaxMm: number;
}

// The template's overall plan box — what the placement ghost shows and what the click centres.
// Rotated bodies contribute all four corners (the composition drafts use 0/90°, but nothing here
// assumes that); an arc run is boxed by its chord band, which the compositions keep shallow.
export const templatePlanBounds = (template: Omit<GlassTemplate, 'key'>): TemplatePlanBounds => {
  let minX = Number.POSITIVE_INFINITY;
  let maxX = Number.NEGATIVE_INFINITY;
  let minY = Number.POSITIVE_INFINITY;
  let maxY = Number.NEGATIVE_INFINITY;
  let zMax = 0;
  const band = (
    originX: number,
    originY: number,
    rotationDeg: number,
    lengthMm: number,
    halfWidthMm: number,
  ) => {
    const rad = (rotationDeg * Math.PI) / 180;
    const dx = Math.cos(rad);
    const dy = Math.sin(rad);
    const px = -dy * halfWidthMm;
    const py = dx * halfWidthMm;
    for (const [ex, ey] of [
      [originX + px, originY + py],
      [originX - px, originY - py],
      [originX + lengthMm * dx + px, originY + lengthMm * dy + py],
      [originX + lengthMm * dx - px, originY + lengthMm * dy - py],
    ]) {
      minX = Math.min(minX, ex);
      maxX = Math.max(maxX, ex);
      minY = Math.min(minY, ey);
      maxY = Math.max(maxY, ey);
    }
  };
  for (const w of template.walls) {
    band(w.originX, w.originY, w.rotationDeg, w.lengthMm, w.thicknessMm / 2);
    zMax = Math.max(zMax, (w.geomZ ?? 0) + Math.max(w.heightMm, w.heightEndMm ?? w.heightMm));
  }
  for (const s of template.slabs) {
    const rad = (s.rotationDeg * Math.PI) / 180;
    // The slab origin is its length-edge start; the body extends depthMm along the left normal —
    // box it via a band whose centreline runs through the slab middle.
    band(
      s.originX - Math.sin(rad) * (s.depthMm / 2),
      s.originY + Math.cos(rad) * (s.depthMm / 2),
      s.rotationDeg,
      s.lengthMm,
      s.depthMm / 2,
    );
    zMax = Math.max(
      zMax,
      s.elevationMm + s.thicknessMm + Math.max(s.arcRiseMm ?? 0, s.pitchRiseMm ?? 0),
    );
  }
  for (const r of template.runs) {
    band(r.originX, r.originY, r.rotationDeg, r.lengthMm, 25);
    zMax = Math.max(zMax, r.heightMm);
  }
  if (!Number.isFinite(minX)) return { minXMm: 0, maxXMm: 0, minYMm: 0, maxYMm: 0, zMaxMm: 0 };
  return { minXMm: minX, maxXMm: maxX, minYMm: minY, maxYMm: maxY, zMaxMm: zMax };
};
