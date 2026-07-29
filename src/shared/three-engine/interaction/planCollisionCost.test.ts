import { describe, expect, it } from 'vitest';
import { buildPlanFootprint, slidePlanMove } from './planCollision';
import type { PlanFootprint } from './planCollision';

/**
 * The drag path runs on EVERY pointermove, which fires at the pointing device's polling rate
 * (125 Hz on a plain mouse, up to 1000 Hz on a gaming one). These tests pin how much work a single
 * solve costs, because that cost is multiplied by the event rate.
 */

const bar = (id: string, x: number, y: number, lengthMm = 3000, halfWidthMm = 50): PlanFootprint =>
  buildPlanFootprint(id, x, y, lengthMm, 0, halfWidthMm, 0, 2500);

/** A scene with a lot of bodies, only a couple of which are anywhere near the drag. */
const farAwayScene = (count: number): PlanFootprint[] =>
  Array.from({ length: count }, (_, i) => bar(`far-${i}`, 100000 + i * 5000, 100000, 3000, 25));

describe('drag-solve cost', () => {
  const measure = (obstacles: PlanFootprint[], dxMm: number, dyMm: number) => {
    let footprintBuilds = 0;
    const moving = bar('moving', 0, 0);
    const footprintAt = (dx: number, dy: number) => {
      footprintBuilds += 1;
      return buildPlanFootprint('moving', dx, dy, 3000, 0, 50, 0, 2500);
    };
    void moving;
    slidePlanMove(footprintAt, obstacles, dxMm, dyMm);
    return footprintBuilds;
  };

  it('a clear 3 m drag past far-away bodies stays cheap', () => {
    // The thinnest obstacle sets the sweep step, so a 25 mm-half-width glass run anywhere in the
    // scene used to force ceil(3000/25) = 120 steps — even when every obstacle was kilometres away.
    const cost = measure(farAwayScene(40), 3000, 0);
    expect(cost).toBeLessThanOrEqual(12);
  });

  it('cost does not grow with how far you have already dragged', () => {
    // slidePlanMove is called with the TOTAL delta since drag start, so an O(pathLen) rescan means
    // the drag gets heavier the longer it runs — the "it gets laggier as I move" symptom.
    const shortDrag = measure(farAwayScene(40), 500, 0);
    const longDrag = measure(farAwayScene(40), 10000, 0);
    expect(longDrag).toBeLessThanOrEqual(shortDrag * 3);
  });

  it('cost does not grow with bodies that are nowhere near the path', () => {
    const small = measure(farAwayScene(5), 3000, 0);
    const large = measure(farAwayScene(200), 3000, 0);
    expect(large).toBeLessThanOrEqual(small * 2);
  });

  it('still blocks a body that is genuinely in the way', () => {
    // The optimisation must not buy speed by forgetting to collide.
    const blocker = bar('blocker', 1500, 0, 200, 100);
    const moving = (dx: number, dy: number) =>
      buildPlanFootprint('moving', dx, dy, 500, 0, 50, 0, 2500);

    const slid = slidePlanMove(moving, [blocker], 3000, 0);

    expect(slid.dxMm).toBeLessThan(3000);
  });

  it('still refuses to tunnel through a thin body', () => {
    // A single pointermove can jump metres; the sweep exists so the body cannot skip over a thin
    // obstacle between its old and new position.
    const thin = bar('thin', 2000, 0, 4000, 10);
    // The moving body must actually cross the thin bar in x, or there is nothing to tunnel through.
    const moving = (dx: number, dy: number) =>
      buildPlanFootprint('moving', 3000 + dx, -3000 + dy, 200, 90, 50, 0, 2500);

    const slid = slidePlanMove(moving, [thin], 0, 6000);

    expect(slid.dyMm).toBeLessThan(6000);
  });
});
