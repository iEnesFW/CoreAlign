import { describe, expect, it } from 'vitest';
import { collectHeightLevels, snapToLevels } from './levelSnap';
import type { SceneState } from '../../model/project.types';

/**
 * The level list is a set of ABSOLUTE Z heights the scene really has something at. The wall branch
 * used to add `heightMm` — a relative size — and ignore `geomZ`, so a raised wall contributed a
 * phantom level where nothing exists and no level where its top actually is.
 */

const scene = (parts: Partial<SceneState>): SceneState =>
  ({ runs: [], walls: [], slabs: [], surfaces: [], connections: [], ...parts }) as SceneState;

const wall = (id: string, geomZ: number, heightMm = 2600, heightEndMm: number | null = null) =>
  ({
    id,
    originX: 0,
    originY: 0,
    rotationDeg: 0,
    lengthMm: 3000,
    thicknessMm: 200,
    heightMm,
    heightEndMm,
    geomZ,
  }) as never;

describe('collectHeightLevels', () => {
  it('offers a raised wall its real base and top, not its bare height', () => {
    const levels = collectHeightLevels(scene({ walls: [wall('w', 400)] }));
    expect(levels).toContain(400);
    expect(levels).toContain(3000);
    // The phantom: nothing in this scene is at 2600.
    expect(levels).not.toContain(2600);
  });

  it('a wall standing on the ground is unchanged — the common case still works', () => {
    const levels = collectHeightLevels(scene({ walls: [wall('w', 0)] }));
    expect(levels).toContain(0);
    expect(levels).toContain(2600);
  });

  it('offsets a sloped wall end height by the base too', () => {
    const levels = collectHeightLevels(scene({ walls: [wall('w', 400, 2600, 2000)] }));
    expect(levels).toContain(2400);
  });

  it('includes drawn SURFACES, which were missing entirely', () => {
    const levels = collectHeightLevels(
      scene({
        surfaces: [{ id: 'deck', kind: 'floor', points: [], elevationMm: 300, thicknessMm: 120 }],
      } as Partial<SceneState>),
    );
    expect(levels).toContain(300);
    expect(levels).toContain(420);
  });

  it('still excludes the body being dragged', () => {
    const levels = collectHeightLevels(scene({ walls: [wall('w', 400)] }), 'w');
    expect(levels).not.toContain(3000);
  });
});

describe('snapToLevels', () => {
  it('magnets a roof to a raised wall top that the old list did not contain', () => {
    const levels = collectHeightLevels(scene({ walls: [wall('w', 400)] }));
    expect(snapToLevels(2985, levels)).toBe(3000);
  });

  it('falls back to the sticky grid when nothing is within tolerance', () => {
    expect(snapToLevels(1234, [0, 3000])).not.toBe(3000);
  });
});
