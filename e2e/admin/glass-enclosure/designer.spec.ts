import { expect, test } from '@playwright/test';
import { skipIfNoStack } from '../../fixtures/credentials';
import { GlassDesignerPage, GlassProjectsPage } from '../../pages';

const openFirstProjectDesigner = async (
  page: import('@playwright/test').Page,
): Promise<GlassDesignerPage | null> => {
  const projects = new GlassProjectsPage(page);
  await projects.goto();
  const hasProject = await projects
    .firstProjectLink()
    .isVisible({ timeout: 5_000 })
    .catch(() => false);
  if (!hasProject) return null;
  await projects.openFirstProject();
  const designer = GlassDesignerPage.fromUrl(page);
  await designer.expectDesignerLoaded();
  return designer;
};

test.describe('admin / glass-enclosure / designer', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test('designer page renders the canvas workspace', async ({ page }) => {
    const designer = await openFirstProjectDesigner(page);
    if (!designer) {
      test.skip(true, 'No glass project to open in this fixture.');
      return;
    }
    await expect(designer.canvasPanel).toBeVisible();
    await expect(designer.runsPanel).toBeVisible();
  });

  test('3D view mounts a webgl canvas', async ({ page }) => {
    const designer = await openFirstProjectDesigner(page);
    if (!designer) {
      test.skip(true, 'No glass project to open in this fixture.');
      return;
    }
    await designer.switchTo3D();
    await designer.expectCanvasMounted();
  });

  test('adds a run ("cephe") and the run list grows', { tag: '@mutation' }, async ({ page }) => {
    const designer = await openFirstProjectDesigner(page);
    if (!designer) {
      test.skip(true, 'No glass project to open in this fixture.');
      return;
    }
    const before = await designer.runCount();
    await designer.addRun();
    await expect(designer.runListItems).toHaveCount(before + 1, { timeout: 15_000 });
  });

  test('adds a panel to a run via rebalance', { tag: '@mutation' }, async ({ page }) => {
    const designer = await openFirstProjectDesigner(page);
    if (!designer) {
      test.skip(true, 'No glass project to open in this fixture.');
      return;
    }
    if ((await designer.runCount()) === 0) {
      await designer.addRun();
      await expect(designer.runListItems).toHaveCount(1, { timeout: 15_000 });
    }
    await designer.selectFirstRun();
    const rebalanceVisible = await designer.rebalancePanelsButton
      .isVisible({ timeout: 5_000 })
      .catch(() => false);
    if (!rebalanceVisible) {
      test.skip(true, 'Inspector rebalance control not available in this layout.');
      return;
    }
    await designer.setPanelCountAndApply(4);
    await expect(designer.runsPanel).toBeVisible();
  });

  test(
    'two added runs are not stacked at identical plan positions',
    { tag: '@mutation' },
    async ({ page }) => {
      const designer = await openFirstProjectDesigner(page);
      if (!designer) {
        test.skip(true, 'No glass project to open in this fixture.');
        return;
      }
      const before = await designer.runCount();
      await designer.addRun();
      await designer.addRun();
      await expect(designer.runListItems).toHaveCount(before + 2, { timeout: 15_000 });

      const segments = await designer.runSegmentCoordinates();
      expect(segments.length).toBeGreaterThanOrEqual(2);

      const fingerprints = segments.map((coords) => coords.join(':'));
      const uniqueFingerprints = new Set(fingerprints);
      expect(uniqueFingerprints.size).toBe(fingerprints.length);
    },
  );
});
