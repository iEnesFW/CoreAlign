import { deriveArcFromSweep } from './arcGeometry';
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
      // Chord along +x, bulge toward +y (canonical: bulge opposite the sweep sign → sweep −90),
      // rotationDeg = the ROLLED start tangent (chordDeg − dir·sweep/2 = 0 − (−1)·90/2 = +45).
      const derived = deriveArcFromSweep(W, 90);
      return {
        key,
        walls: [],
        slabs: [],
        runs: [
          {
            originX: 0,
            originY: 0,
            rotationDeg: 45,
            lengthMm: derived.chordMm,
            heightMm: H,
            geomArcRadiusMm: derived.radiusMm,
            geomArcSweepDeg: -Math.abs(derived.sweepDeg),
            arcGlassBent: true,
          },
        ],
      };
    }
  }
};
