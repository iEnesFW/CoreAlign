import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';

// Numerical verification of the login-free /dev/glass-fixture route. Capturing the WebGL canvas via
// Playwright is a known dead end for this app (R3F emits "Context Lost" under automation), so instead
// of a pixel screenshot we assert the AUTHORITATIVE store geometry exposed by window.__CAD_SCENE__()
// — the reliable "numerical" layer of the verification pyramid. This drives the SAME designer
// pipeline (real CanvasPanel + real catalogs) the audit fixes touch, so the fixture rendering the
// expected arc/panel/hardware geometry confirms the pipeline end to end.
//
// Requires the backend up (the fixture fetches the real profile/glass/color catalogs) plus the Vite
// dev server (started by the e2e webServer). Run: npm run e2e:admin -- dev-fixture

interface CadArc {
  radiusMm: number;
  arcLengthMm: number;
}
interface CadPanel {
  shapeKind?: string | null;
  shapePointsJson?: string | null;
  hasHandle?: boolean;
  hasLock?: boolean;
}
interface CadScene {
  runCount: number;
  derived: { arcs: CadArc[] };
  scene: { runs: { panels: CadPanel[] }[] };
}

const readScene = async (page: Page): Promise<CadScene> => {
  await page.waitForSelector('canvas', { timeout: 20_000 });
  // Wait until the fixture auto-login has fetched the real catalogs and injected the scene.
  await page.waitForFunction(
    () => {
      const fn = (window as unknown as { __CAD_SCENE__?: () => { runCount: number } })
        .__CAD_SCENE__;
      return typeof fn === 'function' && (fn().runCount ?? 0) > 0;
    },
    { timeout: 30_000 },
  );
  return page.evaluate(
    () => (window as unknown as { __CAD_SCENE__: () => CadScene }).__CAD_SCENE__() as unknown,
  ) as Promise<CadScene>;
};

test.describe('glass /dev/glass-fixture — numerical scene verification', () => {
  test('arc-holefill-triangle renders the arc + shaped triangle pane', async ({ page }) => {
    await page.goto('/dev/glass-fixture?scene=arc-holefill-triangle', { waitUntil: 'load' });
    const scene = await readScene(page);

    expect(scene.runCount).toBe(1);
    // One curved run: radius 3000mm, sweep 40° → developed length = 3000·(40π/180) ≈ 2094mm.
    expect(scene.derived.arcs).toHaveLength(1);
    expect(scene.derived.arcs[0].radiusMm).toBeGreaterThan(2900);
    expect(scene.derived.arcs[0].radiusMm).toBeLessThan(3100);
    expect(scene.derived.arcs[0].arcLengthMm).toBeGreaterThan(2000);
    expect(scene.derived.arcs[0].arcLengthMm).toBeLessThan(2200);

    // The single pane is the polygon (triangle) hole-fill — the B2 concave arc-fill path.
    const panel = scene.scene.runs[0].panels[0];
    expect(panel.shapeKind).toBe('polygon');
    expect(panel.shapePointsJson).toBeTruthy();
  });

  test('straight-run renders 3 rectangular panes with hardware on the middle pane', async ({
    page,
  }) => {
    await page.goto('/dev/glass-fixture?scene=straight-run', { waitUntil: 'load' });
    const scene = await readScene(page);

    expect(scene.runCount).toBe(1);
    expect(scene.derived.arcs).toHaveLength(0); // straight run — no arc geometry
    const panels = scene.scene.runs[0].panels;
    expect(panels).toHaveLength(3);
    // The middle pane carries a handle + lock (fixtures.ts straightRun).
    expect(panels[1].hasHandle).toBe(true);
    expect(panels[1].hasLock).toBe(true);
  });
});
