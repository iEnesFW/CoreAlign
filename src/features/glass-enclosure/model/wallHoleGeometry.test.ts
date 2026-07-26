import { describe, expect, it } from 'vitest';
import { computeWallFillPlan } from './wallAutofill';
import { clampOpeningRectMm, resolveWallArc, resolveWallHoles } from './wallHoleGeometry';
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

const reasons = (w: SceneWallState) => resolveWallHoles(w).skipped.map((s) => s.reason);

describe('clampOpeningRectMm', () => {
  it('keeps an interior opening at its exact size', () => {
    const rect = clampOpeningRectMm(opening(), 4000, 2400, 2400);
    expect(rect).not.toBeNull();
    expect(rect?.x0).toBe(1400);
    expect(rect?.x1).toBe(2600);
    expect(rect?.y0).toBe(900);
    expect(rect?.y1).toBe(2100);
  });

  it('leaves a 10 mm head under the roofline', () => {
    const rect = clampOpeningRectMm(opening({ sillMm: 0, heightMm: 2400 }), 4000, 2400, 2400);
    expect(rect?.y1).toBe(2390);
    expect(rect?.y0).toBe(1);
  });

  it('follows the lower end of a sloped wall', () => {
    const rect = clampOpeningRectMm(opening({ sillMm: 0, heightMm: 2400 }), 4000, 2400, 1200);
    // Head limit is the lower of the two edge heights, minus the 10 mm margin.
    const expected = Math.min(2400 - 0.3 * 1400, 2400 - 0.3 * 2600) - 10;
    expect(rect?.y1).toBeCloseTo(expected, 6);
  });

  it('returns null when the wall is genuinely too short at that offset', () => {
    expect(clampOpeningRectMm(opening({ sillMm: 0, heightMm: 1200 }), 4000, 25, 25)).toBeNull();
  });

  it('returns null when the opening is entirely past the wall end', () => {
    expect(
      clampOpeningRectMm(opening({ offsetMm: 4600, widthMm: 1200 }), 4000, 2400, 2400),
    ).toBeNull();
  });
});

describe('resolveWallHoles reports why a hole was refused', () => {
  it('bent wall: nothing is carved', () => {
    const w = wall({ bendAngleDeg: 90, bendAtMm: 2000, openings: [opening()] });
    expect(resolveWallHoles(w).holes).toHaveLength(0);
    expect(reasons(w)).toEqual(['bentWall']);
  });

  it('curved wall: an opening is not a hole', () => {
    const w = wall({ geomArcRadiusMm: 3000, geomArcSweepDeg: 60, openings: [opening()] });
    expect(resolveWallHoles(w).holes).toHaveLength(0);
    expect(reasons(w)).toEqual(['arcOpening']);
  });

  it('side-face feature', () => {
    expect(reasons(wall({ features: [feature({ side: 'top' })] }))).toEqual(['sideFace']);
  });

  it('feature inside the edge margin', () => {
    expect(reasons(wall({ features: [feature({ offsetMm: 505, widthMm: 1000 })] }))).toEqual([
      'notCarved',
    ]);
  });

  it('second opening inside the 50 mm gap', () => {
    const w = wall({
      openings: [
        opening({ id: 'a', offsetMm: 1000, widthMm: 1000 }),
        opening({ id: 'b', offsetMm: 2020, widthMm: 1000 }),
      ],
    });
    expect(resolveWallHoles(w).holes.map((h) => h.id)).toEqual(['a']);
    expect(reasons(w)).toEqual(['notCarved']);
  });

  it('an ellipse hole is glazed but flagged as approximate', () => {
    const w = wall({ features: [feature({ shape: 'ellipse' })] });
    expect(resolveWallHoles(w).holes).toHaveLength(1);
    expect(reasons(w)).toEqual(['approximated']);
  });

  it('a rectangular hole with clearance is filled with no advisory', () => {
    const w = wall({ openings: [opening()], features: [feature({ offsetMm: 3200 })] });
    expect(resolveWallHoles(w).holes).toHaveLength(2);
    expect(reasons(w)).toEqual([]);
  });

  it('a polygon hole keeps its silhouette', () => {
    const w = wall({ features: [feature({ shape: 'triangle' })] });
    const [hole] = resolveWallHoles(w).holes;
    expect(hole.shape?.shapeKind).toBe('polygon');
    expect(hole.shape?.shapePointsJson).toBeTruthy();
  });
});

describe('curved wall fill is produced at DEVELOPED length', () => {
  const curved = wall({
    lengthMm: 3000,
    geomArcRadiusMm: 3000,
    geomArcSweepDeg: 60,
    features: [feature({ offsetMm: 1500, widthMm: 1200 })],
  });

  it('the sub-arc developed length equals the hole width, not the chord', () => {
    const arc = resolveWallArc(curved);
    expect(arc).not.toBeNull();
    const [edge] = computeWallFillPlan([curved]).edges;
    const sweepRad = Math.abs(((edge.geomArcSweepDeg ?? 0) * Math.PI) / 180);
    const developed = (edge.geomArcRadiusMm ?? 0) * sweepRad;
    expect(developed).toBeCloseTo(1200, 0);
    // The chord is strictly shorter — filling at the chord would under-order the glass.
    expect(edge.lengthMm).toBeLessThan(developed);
  });

  it('the wall arc is re-derived from the chord, matching the renderer', () => {
    const arc = resolveWallArc(curved);
    // chord = 2R·sin(sweep/2) must reproduce the stored wall length.
    const chord = 2 * (arc?.radiusMm ?? 0) * Math.sin((arc?.sweepRad ?? 0) / 2);
    expect(chord).toBeCloseTo(curved.lengthMm, 3);
  });

  it('the pane is born as an arc in one shot (radius and sweep on the same edge)', () => {
    const [edge] = computeWallFillPlan([curved]).edges;
    expect(edge.geomArcRadiusMm).toBeGreaterThanOrEqual(100);
    expect(edge.geomArcSweepDeg).not.toBe(0);
    expect(edge.arcGlassBent).toBe(true);
  });
});

describe('curved wall: the pane matches the CARVED outline, not the nominal box', () => {
  const curvedWall = (f: Partial<SceneWallFeature>) =>
    wall({
      lengthMm: 2828,
      geomArcRadiusMm: 2000,
      geomArcSweepDeg: 90,
      heightMm: 3000,
      features: [feature({ offsetMm: 1571, centerZMm: 1500, widthMm: 1000, heightMm: 1000, ...f })],
    });

  // A hexagon inscribed in a 1000x1000 box is only 2*sin(60°)*500 = 866 mm tall.
  it('a hexagon hole is glazed at its outline height, not the nominal 1000', () => {
    const [hole] = resolveWallHoles(curvedWall({ shape: 'polygon', sides: 6 })).holes;
    expect(hole.zHeightMm).toBeCloseTo(866.03, 1);
    expect(hole.zBottomMm).toBeCloseTo(1066.99, 1);
  });

  // An odd n-gon is narrower AND off-centre, so the sub-arc must start later too.
  it('a pentagon hole is glazed at its outline width and offset', () => {
    const [hole] = resolveWallHoles(curvedWall({ shape: 'polygon', sides: 5 })).holes;
    expect(hole.uWidthMm).toBeCloseTo(904.51, 1);
    expect(hole.uStartMm).toBeCloseTo(1166.49, 1);
  });

  it('the straight wall and the curved wall agree for the same feature', () => {
    const f = feature({ shape: 'polygon', sides: 6, offsetMm: 1571, centerZMm: 1500 });
    const straight = resolveWallHoles(wall({ lengthMm: 4000, heightMm: 3000, features: [f] }));
    const curved = resolveWallHoles(curvedWall({ shape: 'polygon', sides: 6 }));
    expect(curved.holes[0].zBottomMm).toBeCloseTo(straight.holes[0].zBottomMm, 1);
    expect(curved.holes[0].zHeightMm).toBeCloseTo(straight.holes[0].zHeightMm, 1);
  });

  it('a deep recess is NOT a through hole on a curved wall (the band keeps a skin)', () => {
    const w = curvedWall({ mode: 'recess', depthMm: 200 });
    expect(resolveWallHoles(w).holes).toHaveLength(0);
    expect(resolveWallHoles(w).skipped.map((s) => s.reason)).toEqual(['notCarved']);
  });

  it('a hole reaching past the wall top is clipped to the band, not glazed above it', () => {
    const w = wall({
      lengthMm: 2828,
      geomArcRadiusMm: 2000,
      geomArcSweepDeg: 90,
      heightMm: 2400,
      features: [feature({ offsetMm: 1571, centerZMm: 2200, widthMm: 800, heightMm: 1000 })],
    });
    const [hole] = resolveWallHoles(w).holes;
    expect(hole.zBottomMm).toBe(1700);
    expect(hole.zBottomMm + hole.zHeightMm).toBe(2400);
  });
});

describe('fill plan idempotency', () => {
  it('a hole already covered by a run is reported, not re-filled', () => {
    const w = wall({ openings: [opening()] });
    const first = computeWallFillPlan([w]);
    expect(first.edges).toHaveLength(1);
    const edge = first.edges[0];
    const run = {
      id: 'r1',
      orderIndex: 0,
      label: 'r',
      lengthMm: edge.lengthMm,
      heightMm: edge.heightMm ?? 0,
      originX: edge.originX,
      originY: edge.originY,
      rotationDeg: edge.rotationDeg,
      profileSystemId: 'ps',
      colorId: null,
      hasTopDrip: false,
      hasBottomThreshold: false,
      geomZ: edge.geomZ ?? 0,
      geomArcRadiusMm: null,
      geomArcSweepDeg: null,
      panels: [],
    };
    const second = computeWallFillPlan([w], [run]);
    expect(second.edges).toHaveLength(0);
    expect(second.skipped.map((s) => s.reason)).toEqual(['alreadyFilled']);
  });

  it('a refused hole does not also claim it was approximated', () => {
    // An ellipse hole carries an 'approximated' advisory from the resolver. Once the plan refuses
    // that same hole, the advisory would claim a fill that never happened — and count it twice.
    const w = wall({ features: [feature({ shape: 'ellipse', widthMm: 800, heightMm: 800 })] });
    const first = computeWallFillPlan([w]);
    expect(first.edges).toHaveLength(1);
    expect(first.skipped.map((s) => s.reason)).toEqual(['approximated']);

    const edge = first.edges[0];
    const run = {
      id: 'r1',
      orderIndex: 0,
      label: 'r',
      lengthMm: edge.lengthMm,
      heightMm: edge.heightMm ?? 0,
      originX: edge.originX,
      originY: edge.originY,
      rotationDeg: edge.rotationDeg,
      profileSystemId: 'ps',
      colorId: null,
      hasTopDrip: false,
      hasBottomThreshold: false,
      geomZ: edge.geomZ ?? 0,
      geomArcRadiusMm: null,
      geomArcSweepDeg: null,
      panels: [],
    };
    const second = computeWallFillPlan([w], [run]);
    expect(second.edges).toHaveLength(0);
    expect(second.skipped.map((s) => s.reason)).toEqual(['alreadyFilled']);
  });
});
