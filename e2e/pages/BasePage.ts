import { expect, type Locator, type Page } from '@playwright/test';

export abstract class BasePage {
  protected constructor(
    protected readonly page: Page,
    protected readonly path: string,
  ) {}

  async goto(): Promise<void> {
    await this.page.goto(this.path);
  }

  get heading(): Locator {
    return this.page.getByRole('heading').first();
  }

  async expectLoaded(headingPattern: RegExp): Promise<void> {
    await expect(this.page.getByRole('heading', { name: headingPattern }).first()).toBeVisible();
  }

  toast(): Locator {
    return this.page.locator('[data-sonner-toast], [role="status"]');
  }

  async waitForUrl(pattern: RegExp): Promise<void> {
    await this.page.waitForURL(pattern, { timeout: 20_000 });
  }
}
