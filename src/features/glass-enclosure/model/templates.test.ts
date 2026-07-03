import { describe, expect, it } from 'vitest';
import { buildGlassTemplate, DEFAULT_TEMPLATE_PARAMS } from './templates';
import { arcEndLocal, developedLengthMm } from './arcGeometry';
import { slabArcDefaultSweepSign } from '../scene/builders/curvedSlabGeometry';

const { widthMm: W, depthMm: D, heightMm: H } = DEFAULT_TEMPLATE_PARAMS;

describe('buildGlassTemplate', () => {
  it('l-walls chains the second wall from the end of the first', () => {
    const tpl = buildGlassTemplate('l-walls');
    expect(tpl.walls).toHaveLength(2);
    expect(tpl.walls[0]).toMatchObject({ originX: 0, originY: 0, rotationDeg: 0, lengthMm: W });
    expect(tpl.walls[1]).toMatchObject({ originX: W, originY: 0, rotationDeg: 90, lengthMm: D });
  });

  it('u-walls forms three connected walls', () => {
    const tpl = buildGlassTemplate('u-walls');
    expect(tpl.walls).toHaveLength(3);
    expect(tpl.walls[1]).toMatchObject({ originX: 0, originY: D, rotationDeg: 0, lengthMm: W });
    expect(tpl.walls[2]).toMatchObject({ originX: W, originY: D, rotationDeg: -90, lengthMm: D });
  });

  it('room-door centers a door opening on the front wall', () => {
    const tpl = buildGlassTemplate('room-door');
    expect(tpl.walls).toHaveLength(4);
    const openings = tpl.walls[0].openings ?? [];
    expect(openings).toHaveLength(1);
    expect(openings[0]).toMatchObject({
      kind: 'door',
      offsetMm: Math.round(W / 2),
      widthMm: 900,
      heightMm: 2050,
    });
    expect(tpl.walls.slice(1).every((w) => (w.openings ?? []).length === 0)).toBe(true);
  });

  it('roof templates elevate the slab and carry their shape fields', () => {
    const gable = buildGlassTemplate('gable-roof').slabs[0];
    expect(gable.elevationMm).toBe(H);
    expect(gable.pitchRiseMm).toBeGreaterThan(0);
    expect(gable.pitchType).toBe('symmetric');
    const barrel = buildGlassTemplate('barrel-roof').slabs[0];
    expect(barrel.arcRiseMm).toBeGreaterThan(0);
  });

  it('arc-roof uses the canonical slab sweep sign and a valid radius', () => {
    const slab = buildGlassTemplate('arc-roof').slabs[0];
    expect(slab.slabArcAxis).toBe('length');
    expect(Math.sign(slab.geomArcSweepDeg ?? 0)).toBe(slabArcDefaultSweepSign('length'));
    expect(slab.geomArcRadiusMm ?? 0).toBeGreaterThanOrEqual(100);
  });

  it('arc-run rolled tangent lands the chord along +x', () => {
    const run = buildGlassTemplate('arc-run').runs[0];
    expect(run.geomArcSweepDeg).toBeLessThan(0);
    const end = arcEndLocal(run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 0);
    const rad = (run.rotationDeg * Math.PI) / 180;
    const worldX = end.xMm * Math.cos(rad) - end.yMm * Math.sin(rad);
    const worldY = end.xMm * Math.sin(rad) + end.yMm * Math.cos(rad);
    expect(worldX).toBeCloseTo(W, 0);
    expect(Math.abs(worldY)).toBeLessThan(2);
    const developed = developedLengthMm(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg);
    expect(developed).toBeGreaterThan(run.lengthMm);
  });

  it('scales to custom params', () => {
    const tpl = buildGlassTemplate('l-walls', { widthMm: 5000, depthMm: 4000, heightMm: 3000 });
    expect(tpl.walls[0].lengthMm).toBe(5000);
    expect(tpl.walls[1]).toMatchObject({ originX: 5000, lengthMm: 4000, heightMm: 3000 });
  });
});
