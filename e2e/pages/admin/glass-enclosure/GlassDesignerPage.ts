import { expect, type Locator, type Page } from '@playwright/test';
import { BasePage } from '../../BasePage';

export class GlassDesignerPage extends BasePage {
  constructor(page: Page, projectId: string) {
    super(page, `/dashboard/glass-enclosure/projects/${projectId}`);
  }

  static fromUrl(page: Page): GlassDesignerPage {
    const id = new URL(page.url()).pathname.split('/').pop() ?? '';
    return new GlassDesignerPage(page, id);
  }

  get runsPanel(): Locator {
    return this.page.locator('[data-tour="designer-runs"]');
  }

  get canvasPanel(): Locator {
    return this.page.locator('[data-tour="designer-canvas"]');
  }

  get threeCanvas(): Locator {
    return this.page.locator('canvas');
  }

  get addRunButton(): Locator {
    return this.page
      .getByRole('button', { name: /add run|cephe ekle|new run|yeni cephe|run ekle/i })
      .first();
  }

  get saveButton(): Locator {
    return this.page.getByRole('button', { name: /save|kaydet/i }).first();
  }

  get validateButton(): Locator {
    return this.page.getByRole('button', { name: /validate|doğrula/i }).first();
  }

  get view3dButton(): Locator {
    return this.page.getByRole('button', { name: /3d/i }).first();
  }

  get runListItems(): Locator {
    return this.runsPanel.locator('ul[role="list"] li button[aria-pressed]');
  }

  panelMeshes(): Locator {
    return this.page.locator('canvas');
  }

  get planCanvas(): Locator {
    return this.canvasPanel.locator('svg').first();
  }

  get runSegments(): Locator {
    return this.planCanvas.locator('line[stroke-width="60"], line[stroke-width="90"]');
  }

  async runSegmentCoordinates(): Promise<Array<[number, number, number, number]>> {
    return this.runSegments.evaluateAll((nodes) =>
      nodes.map((node) => [
        Number(node.getAttribute('x1') ?? '0'),
        Number(node.getAttribute('y1') ?? '0'),
        Number(node.getAttribute('x2') ?? '0'),
        Number(node.getAttribute('y2') ?? '0'),
      ]),
    );
  }

  async expectDesignerLoaded(): Promise<void> {
    await expect(this.canvasPanel).toBeVisible({ timeout: 20_000 });
  }

  async switchTo3D(): Promise<void> {
    await this.view3dButton.click();
  }

  async expectCanvasMounted(): Promise<void> {
    await expect(this.threeCanvas.first()).toBeVisible({ timeout: 30_000 });
  }

  async addRun(): Promise<void> {
    await this.addRunButton.click();
  }

  async runCount(): Promise<number> {
    return this.runListItems.count();
  }

  async selectFirstRun(): Promise<void> {
    await this.runListItems.first().click();
  }

  get panelCountInput(): Locator {
    return this.page.locator('input[type="number"]').first();
  }

  get rebalancePanelsButton(): Locator {
    return this.page.getByRole('button', { name: /rebalance|uygula|apply|yeniden/i }).first();
  }

  async setPanelCountAndApply(count: number): Promise<void> {
    await this.panelCountInput.fill(String(count));
    await this.rebalancePanelsButton.click();
  }
}
