import { describe, expect, it } from 'vitest';
import { applyWallStack } from './stackCoMove';
import type { SceneRunState, SceneState, SceneWallState } from './project.types';

const wall = (id: string, originX: number, geomZ: number, groupId?: string): SceneWallState => ({
  id,
  originX,
  originY: 0,
  lengthMm: 2000,
  rotationDeg: 0,
  heightMm: 2600,
  heightEndMm: null,
  thicknessMm: 200,
  colorHex: null,
  openings: [],
  features: [],
  geomZ,
  ...(groupId ? { groupId } : {}),
});

const run = (id: string, originX: number, geomZ: number): SceneRunState => ({
  id,
  orderIndex: 0,
  label: id,
  lengthMm: 2000,
  heightMm: 2400,
  originX,
  originY: 0,
  rotationDeg: 0,
  profileSystemId: 'ps',
  colorId: null,
  hasTopDrip: true,
  hasBottomThreshold: false,
  geomZ,
  geomArcRadiusMm: null,
  geomArcSweepDeg: null,
  panels: [],
});

const scene = (walls: SceneWallState[], runs: SceneRunState[]): SceneState =>
  ({ walls, runs, slabs: [], surfaces: [], connections: [] }) as unknown as SceneState;

describe('applyWallStack', () => {
  it('lands the stacked wall on the support and carries attached glass by the same delta', () => {
    const s = scene([wall('w1', 0, 0)], [run('r1', 0, 300)]);

    const next = applyWallStack(s, {
      wallId: 'w1',
      dxMm: 100,
      dyMm: 0,
      targetZMm: 2600,
      groupWallIds: [],
      attachedRunIds: ['r1'],
    });

    expect(next.walls?.[0].geomZ).toBe(2600);
    expect(next.walls?.[0].originX).toBe(100);
    // The run kept its 300 mm offset above the wall base instead of being flattened onto it.
    expect(next.runs[0].geomZ).toBe(2900);
    expect(next.runs[0].originX).toBe(100);
  });

  it('carries group siblings by the delta, not to the absolute target', () => {
    const s = scene([wall('w1', 0, 0, 'g'), wall('w2', 3000, 500, 'g')], []);

    const next = applyWallStack(s, {
      wallId: 'w1',
      dxMm: 0,
      dyMm: 50,
      targetZMm: 2600,
      groupWallIds: ['w2'],
      attachedRunIds: [],
    });

    expect(next.walls?.find((w) => w.id === 'w1')?.geomZ).toBe(2600);
    expect(next.walls?.find((w) => w.id === 'w2')?.geomZ).toBe(3100);
    expect(next.walls?.find((w) => w.id === 'w2')?.originY).toBe(50);
  });

  it('leaves unrelated walls and runs untouched', () => {
    const s = scene([wall('w1', 0, 0), wall('other', 9000, 0)], [run('other-run', 9000, 0)]);

    const next = applyWallStack(s, {
      wallId: 'w1',
      dxMm: 100,
      dyMm: 100,
      targetZMm: 2600,
      groupWallIds: [],
      attachedRunIds: [],
    });

    expect(next.walls?.find((w) => w.id === 'other')?.originX).toBe(9000);
    expect(next.walls?.find((w) => w.id === 'other')?.geomZ).toBe(0);
    expect(next.runs[0].originX).toBe(9000);
    expect(next.runs[0].geomZ).toBe(0);
  });

  it('descending back to the ground pulls the attached glass down by the same delta', () => {
    const s = scene([wall('w1', 0, 2600)], [run('r1', 0, 2900)]);

    const next = applyWallStack(s, {
      wallId: 'w1',
      dxMm: 0,
      dyMm: 0,
      targetZMm: 0,
      groupWallIds: [],
      attachedRunIds: ['r1'],
    });

    expect(next.walls?.[0].geomZ).toBe(0);
    expect(next.runs[0].geomZ).toBe(300);
  });

  it('is a no-op when the wall is missing', () => {
    const s = scene([wall('w1', 0, 0)], []);

    const next = applyWallStack(s, {
      wallId: 'nope',
      dxMm: 100,
      dyMm: 0,
      targetZMm: 2600,
      groupWallIds: [],
      attachedRunIds: [],
    });

    expect(next).toBe(s);
  });
});
