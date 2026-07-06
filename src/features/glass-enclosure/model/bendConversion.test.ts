import { describe, expect, it } from 'vitest';
import { computeBendLegs } from './bendConversion';
import { buildWallFootprint, penetratesAny } from '../scene/interaction/planCollision';
import type { SceneWallState } from './project.types';

const wall = (overrides: Partial<SceneWallState> = {}): SceneWallState => ({
  id: 'w',
  originX: 1000,
  originY: 500,
  lengthMm: 4000,
  rotationDeg: 30,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  geomZ: 0,
  openings: [],
  features: [],
  ...overrides,
});

const dir = (deg: number) => {
  const rad = (deg * Math.PI) / 180;
  return { x: Math.cos(rad), y: Math.sin(rad) };
};

describe('computeBendLegs', () => {
  it.each([45, 90, 135, -90])(
    'lands leg B free end on the BendHandle preview endpoint (bend %d°)',
    (bendDeg) => {
      const w = wall();
      const bendAt = 2000;
      const legs = computeBendLegs(w, bendAt, bendDeg);
      expect(legs).not.toBeNull();
      const { legB } = legs ?? {};
      const d1 = dir(w.rotationDeg);
      const corner = { x: w.originX + bendAt * d1.x, y: w.originY + bendAt * d1.y };
      const d2 = dir(w.rotationDeg - bendDeg);
      const expected = {
        x: corner.x + (w.lengthMm - bendAt) * d2.x,
        y: corner.y + (w.lengthMm - bendAt) * d2.y,
      };
      const dB = dir(legB?.rotationDeg ?? 0);
      const freeEnd = {
        x: (legB?.originX ?? 0) + (legB?.lengthMm ?? 0) * dB.x,
        y: (legB?.originY ?? 0) + (legB?.lengthMm ?? 0) * dB.y,
      };
      expect(Math.hypot(freeEnd.x - expected.x, freeEnd.y - expected.y)).toBeLessThan(2.5);
    },
  );

  it('produces a clean contact-only butt joint at ±90° for every wall thickness', () => {
    // Non-90° bends deliberately OVERLAP at the corner (two rectangles cannot form a mitred L
    // without a gap otherwise); the edit paths exclude group siblings from their static collision
    // checks, so only the canonical 90° case must be contact-clean under the raw predicate.
    const failures: string[] = [];
    for (const sign of [1, -1] as const) {
      for (const thicknessMm of [60, 100, 200, 300]) {
        const w = wall({ thicknessMm, rotationDeg: 0, originX: 0, originY: 0 });
        const legs = computeBendLegs(w, 2000, sign * 90);
        expect(legs).not.toBeNull();
        const a = buildWallFootprint(
          legs?.legA as SceneWallState,
          0,
          0,
          legs?.legA.rotationDeg ?? 0,
        );
        const b = buildWallFootprint(
          legs?.legB as SceneWallState,
          0,
          0,
          legs?.legB.rotationDeg ?? 0,
        );
        if (penetratesAny(a, [b])) failures.push(`bend=${sign * 90} t=${thicknessMm}`);
      }
    }
    expect(failures).toEqual([]);
  });

  it('covers the outer elbow: the corner centreline point lies inside leg A for every bend', () => {
    for (let bendDeg = 15; bendDeg <= 175; bendDeg += 20) {
      const w = wall({ thicknessMm: 200, rotationDeg: 0, originX: 0, originY: 0 });
      const legs = computeBendLegs(w, 2000, bendDeg);
      expect(legs?.legA.lengthMm ?? 0).toBeGreaterThanOrEqual(2000);
      const dB = dir(legs?.legB.rotationDeg ?? 0);
      const gap = Math.hypot((legs?.legB.originX ?? 0) - 2000, legs?.legB.originY ?? 0);
      expect(gap).toBeLessThanOrEqual(150);
      expect(dB.x).toBeCloseTo(Math.cos((-bendDeg * Math.PI) / 180), 6);
    }
  });

  it('keeps leg A on the original wall id, shares a group id, and clears bend fields', () => {
    const w = wall({ bendAtMm: 2000, bendAngleDeg: 90 });
    const legs = computeBendLegs(w, 2000, 90);
    expect(legs?.legA.id).toBe(w.id);
    expect(legs?.legB.id).not.toBe(w.id);
    expect(legs?.legA.groupId).toBeTruthy();
    expect(legs?.legA.groupId).toBe(legs?.legB.groupId);
    expect(legs?.legA.bendAngleDeg).toBeNull();
    expect(legs?.legB.bendAngleDeg).toBeNull();
  });

  it('redistributes openings and features to the correct leg with shifted offsets', () => {
    const w = wall({
      openings: [
        { id: 'o1', kind: 'window', offsetMm: 800, widthMm: 600, sillMm: 900, heightMm: 1200 },
        { id: 'o2', kind: 'window', offsetMm: 3000, widthMm: 600, sillMm: 900, heightMm: 1200 },
      ],
      features: [
        {
          id: 'f1',
          shape: 'rect',
          mode: 'recess',
          side: 1,
          offsetMm: 3200,
          centerZMm: 1200,
          widthMm: 400,
          heightMm: 400,
          depthMm: 20,
          colorHex: null,
        },
      ],
    });
    const legs = computeBendLegs(w, 2000, 90);
    expect(legs?.legA.openings?.map((o) => o.id)).toEqual(['o1']);
    expect(legs?.legB.openings?.map((o) => o.id)).toEqual(['o2']);
    const shifted = legs?.legB.openings?.[0]?.offsetMm ?? 0;
    expect(shifted).toBeLessThan(1000);
    expect(shifted).toBeGreaterThan(800);
    expect(legs?.legB.features?.[0]?.offsetMm).toBe(shifted + 200);
  });

  it('rejects a bend point too close to either end', () => {
    expect(computeBendLegs(wall(), 50, 90)).toBeNull();
    expect(computeBendLegs(wall(), 3950, 90)).toBeNull();
    expect(computeBendLegs(wall(), 2000, 0.5)).toBeNull();
  });

  it('rejects a split that crosses an opening or feature span (straddle), but allows an edge butt', () => {
    // opening centred on the cut → span [1700, 2300] straddles 2000 → blocked (would be clipped)
    const straddlingOpening = wall({
      openings: [
        { id: 'o', kind: 'window', offsetMm: 2000, widthMm: 600, sillMm: 900, heightMm: 1200 },
      ],
    });
    expect(computeBendLegs(straddlingOpening, 2000, 90)).toBeNull();

    // feature span [1700, 2100] also crosses the cut → blocked
    const straddlingFeature = wall({
      features: [
        {
          id: 'f',
          shape: 'rect',
          mode: 'recess',
          side: 1,
          offsetMm: 1900,
          centerZMm: 1200,
          widthMm: 400,
          heightMm: 400,
          depthMm: 20,
          colorHex: null,
        },
      ],
    });
    expect(computeBendLegs(straddlingFeature, 2000, 90)).toBeNull();

    // opening butting exactly at the cut edge (|offset - cut| === w/2) falls cleanly to one leg
    const buttingOpening = wall({
      openings: [
        { id: 'o', kind: 'window', offsetMm: 1700, widthMm: 600, sillMm: 900, heightMm: 1200 },
      ],
    });
    expect(computeBendLegs(buttingOpening, 2000, 90)).not.toBeNull();
  });
});
