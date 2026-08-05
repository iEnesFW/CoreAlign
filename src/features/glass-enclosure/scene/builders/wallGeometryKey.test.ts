import { describe, expect, it } from 'vitest';
import { wallGeometrySignature } from './wallGeometryKey';
import type { WallGeometryFields } from './wallGeometryKey';

const wall = (over: Partial<WallGeometryFields> = {}): WallGeometryFields => ({
  lengthMm: 4000,
  heightMm: 2600,
  thicknessMm: 200,
  openings: [],
  features: [],
  ...over,
});

describe('wallGeometrySignature', () => {
  it('survives a structuredClone — the identity churn that replayed the CSG', () => {
    const source = wall({
      geomArcRadiusMm: 2000,
      geomArcSweepDeg: 90,
      cornerRadiiMm: { tl: 40, tr: 0 },
      geomEdgeArc: { front: 120 },
      openings: [
        { id: 'o1', kind: 'window', offsetMm: 1500, sillMm: 900, widthMm: 1200, heightMm: 1400 },
      ],
      features: [
        {
          id: 'f1',
          shape: 'rect',
          mode: 'hole',
          side: 1,
          offsetMm: 800,
          centerZMm: 1200,
          widthMm: 600,
          heightMm: 600,
          depthMm: 200,
        },
      ],
    });
    const cloned = structuredClone(source);

    expect(cloned.features).not.toBe(source.features);
    expect(wallGeometrySignature(cloned)).toBe(wallGeometrySignature(source));
  });

  it('ignores fields the geometry does not read', () => {
    const a = wall();
    const b = { ...a, id: 'other', originX: 9999, locked: true, colorHex: '#f00' };
    expect(wallGeometrySignature(b as WallGeometryFields)).toBe(wallGeometrySignature(a));
  });

  it('changes when a hole is drilled, resized or removed', () => {
    const plain = wall();
    const drilled = wall({
      features: [
        {
          id: 'f1',
          shape: 'rect',
          mode: 'hole',
          side: 1,
          offsetMm: 800,
          centerZMm: 1200,
          widthMm: 600,
          heightMm: 600,
          depthMm: 200,
        },
      ],
    });
    const widened = wall({
      features: drilled.features?.map((f) => ({ ...f, widthMm: 900 })),
    });

    expect(wallGeometrySignature(drilled)).not.toBe(wallGeometrySignature(plain));
    expect(wallGeometrySignature(widened)).not.toBe(wallGeometrySignature(drilled));
  });

  it('separates an absent value from an explicit zero', () => {
    expect(wallGeometrySignature(wall({ heightEndMm: 0 }))).not.toBe(
      wallGeometrySignature(wall({ heightEndMm: null })),
    );
  });

  it('distinguishes free-drawn outlines that differ only in a point', () => {
    const feature = {
      id: 'f1',
      shape: 'free' as const,
      mode: 'hole' as const,
      side: 1 as const,
      offsetMm: 800,
      centerZMm: 1200,
      widthMm: 600,
      heightMm: 600,
      depthMm: 200,
    };
    const a = wall({
      features: [
        {
          ...feature,
          points: [
            { x: 0, z: 0 },
            { x: 10, z: 0 },
            { x: 0, z: 10 },
          ],
        },
      ],
    });
    const b = wall({
      features: [
        {
          ...feature,
          points: [
            { x: 0, z: 0 },
            { x: 11, z: 0 },
            { x: 0, z: 10 },
          ],
        },
      ],
    });
    expect(wallGeometrySignature(a)).not.toBe(wallGeometrySignature(b));
  });
});
