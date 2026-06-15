import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '../../BasePage';

export class NewProjectWizardPage extends BasePage {
  static readonly route = '/dashboard/glass-enclosure/projects/new';

  constructor(page: Page) {
    super(page, NewProjectWizardPage.route);
  }

  firstCategoryCard(): Locator {
    return this.page.locator('div.grid > button').first();
  }

  get projectNameInput(): Locator {
    return this.page.locator('#wizard-project-name');
  }

  get customerSearchInput(): Locator {
    return this.page.locator('#wizard-customer-search');
  }

  customerOptionByIndex(index: number): Locator {
    return this.page.locator('ul li button').nth(index);
  }

  get nextButton(): Locator {
    return this.page.getByRole('button', { name: /ileri|next/i });
  }

  get runWidthInput(): Locator {
    return this.page.locator('#run-0-w');
  }

  get runHeightInput(): Locator {
    return this.page.locator('#run-0-h');
  }

  get createButton(): Locator {
    return this.page.getByRole('button', { name: /projeyi oluştur|create project|create/i });
  }

  async pickFirstCategory(): Promise<void> {
    await this.firstCategoryCard().click();
  }

  async fillProjectName(name: string): Promise<void> {
    await this.projectNameInput.fill(name);
  }

  async searchAndSelectFirstCustomer(query: string): Promise<void> {
    await this.customerSearchInput.fill(query);
    await this.customerOptionByIndex(0).click();
  }

  async fillQuickDimensions(widthMm: number, heightMm: number): Promise<void> {
    await this.runWidthInput.fill(String(widthMm));
    await this.runHeightInput.fill(String(heightMm));
  }
}
