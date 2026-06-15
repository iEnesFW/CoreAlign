import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '../../BasePage';

export class GlassProjectsPage extends BasePage {
  static readonly route = '/dashboard/glass-enclosure/projects';
  static readonly headingPattern = /glass|cam|projects|projeler/i;

  constructor(page: Page) {
    super(page, GlassProjectsPage.route);
  }

  get newProjectButton(): Locator {
    return this.page.locator('[data-tour="new-project-button"]');
  }

  get projectsTable(): Locator {
    return this.page.getByRole('table');
  }

  get projectRows(): Locator {
    return this.projectsTable.locator('tbody tr');
  }

  get searchInput(): Locator {
    return this.page.locator('input[type="text"]').first();
  }

  firstProjectLink(): Locator {
    return this.projectRows.first().getByRole('link').first();
  }

  async openNewProjectWizard(): Promise<void> {
    await this.newProjectButton.click();
    await this.waitForUrl(/glass-enclosure\/projects\/new/);
  }

  async openFirstProject(): Promise<void> {
    await this.firstProjectLink().click();
    await this.waitForUrl(/glass-enclosure\/projects\/[^/]+$/);
  }
}
