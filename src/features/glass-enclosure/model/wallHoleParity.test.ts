import { describe, expect, it } from 'vitest';
import { computeOpeningEdges } from './wallAutofill';
import { resolveWallArc, resolveWallHoles } from './wallHoleGeometry';
import {
  FREE_STANDING_DEPTH_MM,
  SHADOW_GAP_MM,
  mountedSection,
  resolveMountDepth,
} from './mountDepth';
import type { SceneWallFeature, SceneWallOpening, SceneWallState } from './project.types';

const wall = (patch: Partial<SceneWallState> = {}): SceneWallState => ({
  id: 'w1',
  originX: 0,
  originY: 0,
  lengthMm: 4000,
  rotationDeg: 0,
  heightMm: 2400,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  openings: [],
  features: [],
  ...patch,
});

const opening = (patch: Partial<SceneWallOpening> = {}): SceneWallOpening => ({
  id: 'o1',
  kind: 'window',
  offsetMm: 2000,
  sillMm: 900,
  widthMm: 1200,
  heightMm: 1200,
  ...patch,
});

const feature = (patch: Partial<SceneWallFeature> = {}): SceneWallFeature => ({
  id: 'f1',
  shape: 'rect',
  mode: 'hole',
  side: 1,
  offsetMm: 2000,
  centerZMm: 1200,
  widthMm: 1000,
  heightMm: 1000,
  depthMm: 200,
  ...patch,
});

// The contract under test: every edge autofill emits must describe a hole the wall ACTUALLY has,
// at the same size and place — and every carved hole must get exactly one edge.
const expectParity = (w: SceneWallState) => {
  const { holes } = resolveWallHoles(w);
  const edges = computeOpeningEdges([w]);
  const baseZ = w.geomZ ?? 0;
  const arc = resolveWallArc(w);

  expect(edges).toHaveLength(holes.length);

  holes.forEach((hole, i) => {
    const edge = edges[i];
    expect(edge.geomZ).toBeCloseTo(Math.round(baseZ + hole.zBottomMm), 0);
    // The TOP edge is the one that must land on the carved hole's top. Rounding the base and the
    // height independently let it drift by two half-millimetres (measured 0.75 mm on a polygon
    // feature hole) — a hairline seam. Both ends are now rounded against the same grid.
    const glassTop = (edge.geomZ ?? 0) + (edge.heightMm ?? 0);
    expect(Math.abs(glassTop - (baseZ + hole.zBottomMm + hole.zHeightMm))).toBeLessThanOrEqual(0.5);
    if (arc) {
      // Curved wall: the pane is a sub-arc, so its DEVELOPED length is the hole's face width.
      const developed =
        (edge.geomArcRadiusMm ?? 0) * Math.abs(((edge.geomArcSweepDeg ?? 0) * Math.PI) / 180);
      expect(Math.abs(developed - hole.uWidthMm)).toBeLessThanOrEqual(1);
      return;
    }
    expect(Math.abs(edge.lengthMm - hole.uWidthMm)).toBeLessThanOrEqual(1);
    const rad = (w.rotationDeg * Math.PI) / 180;
    expect(edge.originX).toBeCloseTo(Math.round(w.originX + hole.uStartMm * Math.cos(rad)), 0);
    expect(edge.originY).toBeCloseTo(Math.round(w.originY + hole.uStartMm * Math.sin(rad)), 0);
  });
};

describe('autofill glass matches the hole the wall actually has', () => {
  it('interior window with clearance on every side', () => {
    expectParity(wall({ openings: [opening()] }));
  });

  it('full-height opening: the wall keeps a 10 mm head, so the glass must too', () => {
    expectParity(wall({ openings: [opening({ sillMm: 0, widthMm: 2400, heightMm: 2400 })] }));
  });

  it('door at sill 0: the carved bottom is 1 mm, not 0', () => {
    expectParity(wall({ openings: [opening({ kind: 'door', sillMm: 0, heightMm: 2100 })] }));
  });

  it('opening flush to the wall start is clamped by the side margin', () => {
    expectParity(wall({ openings: [opening({ offsetMm: 1200, widthMm: 2400 })] }));
  });

  it('opening overhanging the wall end is clamped, not glazed off the wall', () => {
    expectParity(wall({ openings: [opening({ offsetMm: 3800, widthMm: 1200 })] }));
  });

  it('second opening within the 50 mm gap is not carved, so it must not be glazed', () => {
    expectParity(
      wall({
        openings: [
          opening({ id: 'a', offsetMm: 1000, widthMm: 1000 }),
          opening({ id: 'b', offsetMm: 2020, widthMm: 1000 }),
        ],
      }),
    );
  });

  it('sloped wall: the head clamp follows the lower end', () => {
    expectParity(wall({ heightEndMm: 1800, openings: [opening({ sillMm: 400, heightMm: 1600 })] }));
  });

  it('rotated wall keeps the hole on the wall line', () => {
    expectParity(wall({ rotationDeg: 37, openings: [opening()] }));
  });

  it('raised wall lifts the glass by its own base', () => {
    expectParity(wall({ geomZ: 500, openings: [opening()] }));
  });

  it('through-hole feature', () => {
    expectParity(wall({ features: [feature()] }));
  });

  it('feature the wall refuses to carve (inside the edge margin) is not glazed', () => {
    expectParity(wall({ features: [feature({ offsetMm: 505, widthMm: 1000 })] }));
  });

  it('feature contained in an opening is not carved twice', () => {
    expectParity(
      wall({
        openings: [opening({ offsetMm: 2000, widthMm: 2000, sillMm: 400, heightMm: 1600 })],
        features: [feature({ offsetMm: 2000, centerZMm: 1200, widthMm: 600, heightMm: 600 })],
      }),
    );
  });

  it('side-face feature is not a front hole and must not be glazed', () => {
    expectParity(wall({ features: [feature({ side: 'left' })] }));
  });

  it('bent wall carves nothing, so it fills nothing', () => {
    expectParity(wall({ bendAngleDeg: 90, bendAtMm: 2000, openings: [opening()] }));
  });

  it('curved wall: an opening is never carved into the band', () => {
    expectParity(wall({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, openings: [opening()] }));
  });

  it('curved wall: a feature hole is carved and glazed at developed length', () => {
    expectParity(wall({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, features: [feature()] }));
  });
});

/**
 * The THIRD axis. A carved opening runs the full wall thickness; the assembly put back into it must
 * fill that depth, or the pane reads as "not seated" with a visible reveal on both faces.
 */
describe('the fill assembly seats through the wall thickness', () => {
  it('leaves only the deliberate shadow line on each face, whatever the wall', () => {
    for (const thicknessMm of [100, 150, 200, 300, 450]) {
      const mount = resolveMountDepth(thicknessMm);
      const revealPerFace = (thicknessMm - mount.depthMm) / 2;
      expect(revealPerFace).toBeCloseTo(SHADOW_GAP_MM, 6);
      // The OLD fixed 50 mm section left this much open instead — 75 mm on a 200 mm wall.
      expect((thicknessMm - FREE_STANDING_DEPTH_MM) / 2).toBeGreaterThan(revealPerFace);
    }
  });

  it('a free-standing run is untouched by the rule', () => {
    expect(resolveMountDepth(null).depthMm).toBe(FREE_STANDING_DEPTH_MM);
  });

  it('the frame section carries the depth on the wall-normal axis', () => {
    // Bar renders boxGeometry [length, height/1000, width/1000] — `width` IS the wall normal.
    expect(mountedSection(60, resolveMountDepth(200))).toEqual({ width: 180, height: 60 });
  });
});
