import { describe, expect, it } from 'vitest';
import {
  bodyChordMidWorldMm,
  bodyChordVectorMm,
  bodyEndWorldMm,
  bodyMidWorldMm,
  originForChordCentreMm,
} from './curvature';
import { arcBandOutlineMm } from '../model/bandOutline';
import { scenePlanShapes } from '../model/sceneExport';
import type { SceneState } from '../model/project.types';

const DEG2RAD = Math.PI / 180;

/** What every one of these consumers used to do: walk lengthMm along rotationDeg. */
const ghostEnd = (body: {
  originX: number;
  originY: number;
  lengthMm: number;
  rotationDeg: number;
}) => ({
  xMm: body.originX + body.lengthMm * Math.cos(body.rotationDeg * DEG2RAD),
  yMm: body.originY + body.lengthMm * Math.sin(body.rotationDeg * DEG2RAD),
});

// A 4000 mm chord running due east, curved 90 degrees. rotationDeg is the ROLLED start tangent,
// so it points 45 degrees away from the chord.
const arcBody = {
  originX: 0,
  originY: 0,
  lengthMm: 4000,
  rotationDeg: -45,
  geomArcRadiusMm: 2829,
  geomArcSweepDeg: 90,
};

const straightBody = {
  originX: 1000,
  originY: 500,
  lengthMm: 4000,
  rotationDeg: 30,
  geomArcRadiusMm: null,
  geomArcSweepDeg: null,
};

describe('chord accessors', () => {
  it('a straight body is unchanged — the chord IS length along rotationDeg', () => {
    const end = bodyEndWorldMm(straightBody);
    const ghost = ghostEnd(straightBody);
    expect(end.xMm).toBeCloseTo(ghost.xMm, 6);
    expect(end.yMm).toBeCloseTo(ghost.yMm, 6);
  });

  it('an arc body ends METRES away from where the ghost expression puts it', () => {
    const end = bodyEndWorldMm(arcBody);
    const ghost = ghostEnd(arcBody);
    const errorMm = Math.hypot(end.xMm - ghost.xMm, end.yMm - ghost.yMm);
    expect(errorMm).toBeGreaterThan(1000);
  });

  it('the chord of a rolled arc runs along the chord direction, not the tangent', () => {
    const chord = bodyChordVectorMm(arcBody);
    // rotationDeg + sweep/2 = -45 + 45 = 0 degrees, i.e. due east.
    expect(Math.atan2(chord.yMm, chord.xMm) / DEG2RAD).toBeCloseTo(0, 6);
    expect(Math.hypot(chord.xMm, chord.yMm)).toBeCloseTo(4000, 0);
  });

  it('placing by chord centre puts the MIDPOINT on the cursor', () => {
    const placed = { ...arcBody, ...originForChordCentreMm(7000, 3000, arcBody) };
    const mid = bodyChordMidWorldMm(placed);
    expect(mid.xMm).toBeCloseTo(7000, 0);
    expect(mid.yMm).toBeCloseTo(3000, 0);
  });

  it('the old placement expression misses the cursor by over a metre on an arc', () => {
    const rad = arcBody.rotationDeg * DEG2RAD;
    const legacy = {
      ...arcBody,
      originX: Math.round(7000 - (arcBody.lengthMm / 2) * Math.cos(rad)),
      originY: Math.round(3000 - (arcBody.lengthMm / 2) * Math.sin(rad)),
    };
    const mid = bodyChordMidWorldMm(legacy);
    expect(Math.hypot(mid.xMm - 7000, mid.yMm - 3000)).toBeGreaterThan(1000);
  });
});

describe('arcBandOutlineMm', () => {
  it('closes a band whose two ends sit on the real endpoints', () => {
    const half = 50;
    const outline = arcBandOutlineMm(arcBody, 0, 0, arcBody.rotationDeg, half);
    expect(outline.length).toBeGreaterThanOrEqual(14);

    const end = bodyEndWorldMm(arcBody);
    // The outline runs outer-start -> outer-end -> inner-end -> inner-start; the outer and inner
    // samples at the far end straddle the true endpoint by the half width.
    const outerEnd = outline[outline.length / 2 - 1];
    const innerEnd = outline[outline.length / 2];
    const mid = { x: (outerEnd.x + innerEnd.x) / 2, y: (outerEnd.y + innerEnd.y) / 2 };
    expect(Math.hypot(mid.x - end.xMm, mid.y - end.yMm)).toBeLessThan(1);
  });

  it('bulges outside the straight chord rectangle', () => {
    const outline = arcBandOutlineMm(arcBody, 0, 0, arcBody.rotationDeg, 50);
    // The chord runs due east from the origin, so a straight body would never leave |y| <= 50.
    const maxAbsY = Math.max(...outline.map((p) => Math.abs(p.y)));
    expect(maxAbsY).toBeGreaterThan(500);
  });
});

describe('plan export', () => {
  const scene = (over: Partial<SceneState>): SceneState =>
    ({ runs: [], walls: [], slabs: [], surfaces: [], ...over }) as SceneState;

  it('emits a curved wall as a band, not a four-corner rectangle', () => {
    const shapes = scenePlanShapes(
      scene({
        walls: [
          {
            id: 'w1',
            ...arcBody,
            heightMm: 2500,
            thicknessMm: 100,
            openings: [],
            features: [],
          },
        ],
      } as Partial<SceneState>),
    );

    const wallShape = shapes.find((s) => s.layer === 'WALLS');
    expect(wallShape).toBeDefined();
    expect(wallShape!.points.length).toBeGreaterThan(4);
    // A rectangle at the ghost endpoint would have reached here; the real band does not.
    const ghost = ghostEnd(arcBody);
    const nearGhost = wallShape!.points.some(
      (p) => Math.hypot(p.x - ghost.xMm, p.y - ghost.yMm) < 100,
    );
    expect(nearGhost).toBe(false);
  });

  it('still emits a straight wall as a plain rectangle', () => {
    const shapes = scenePlanShapes(
      scene({
        walls: [
          {
            id: 'w2',
            ...straightBody,
            heightMm: 2500,
            thicknessMm: 100,
            openings: [],
            features: [],
          },
        ],
      } as Partial<SceneState>),
    );
    expect(shapes.find((s) => s.layer === 'WALLS')!.points).toHaveLength(4);
  });

  it('honours a bowed surface edge instead of drawing the straight polygon', () => {
    const points = [
      { x: 0, y: 0 },
      { x: 4000, y: 0 },
      { x: 4000, y: 3000 },
      { x: 0, y: 3000 },
    ];
    const straight = scenePlanShapes(
      scene({ surfaces: [{ id: 's1', points }] } as Partial<SceneState>),
    );
    const bowed = scenePlanShapes(
      scene({
        surfaces: [{ id: 's1', points, edgeArcs: [600, null, null, null] }],
      } as Partial<SceneState>),
    );
    expect(straight.find((s) => s.layer === 'SURFACES')!.points).toHaveLength(4);
    expect(bowed.find((s) => s.layer === 'SURFACES')!.points.length).toBeGreaterThan(4);
  });
});

describe('bodyMidWorldMm', () => {
  it('lands ON the band, not on the chord that cuts across it', () => {
    const mid = bodyMidWorldMm(arcBody);
    const chordMid = bodyChordMidWorldMm(arcBody);
    // The apex stands off the chord by the sagitta — that gap is exactly why a chord-midpoint
    // gravity probe asked what was under empty space.
    expect(Math.hypot(mid.xMm - chordMid.xMm, mid.yMm - chordMid.yMm)).toBeGreaterThan(500);
  });

  it('is the plain half-way point on a straight body', () => {
    const mid = bodyMidWorldMm(straightBody);
    const chordMid = bodyChordMidWorldMm(straightBody);
    expect(mid.xMm).toBeCloseTo(chordMid.xMm, 6);
    expect(mid.yMm).toBeCloseTo(chordMid.yMm, 6);
  });

  it('stands off the chord by exactly the sagitta', () => {
    const mid = bodyMidWorldMm(arcBody);
    // The chord runs due east from the origin, so the apex's offset from it is |y|, and for an arc
    // that is R * (1 - cos(sweep/2)) by definition.
    const sagitta =
      arcBody.geomArcRadiusMm * (1 - Math.cos(((arcBody.geomArcSweepDeg / 2) * Math.PI) / 180));
    expect(Math.abs(mid.yMm)).toBeCloseTo(sagitta, 0);
  });
});
