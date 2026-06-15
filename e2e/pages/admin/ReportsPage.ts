import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '../BasePage';

export class ReportsPage extends BasePage {
  static readonly route = '/dashboard/reports';
  static readonly libraryRoute = '/dashboard/reports/library';
  static readonly customRoute = '/dashboard/reports/custom';
  static readonly schedulesRoute = '/dashboard/reports/schedules';
  static readonly headingPattern = /reports|raporlar/i;

  constructor(page: Page) {
    super(page, ReportsPage.route);
  }

  async gotoLibrary(): Promise<void> {
    await this.page.goto(ReportsPage.libraryRoute);
  }

  async gotoCustomBuilder(): Promise<void> {
    await this.page.goto(ReportsPage.customRoute);
  }

  async gotoSchedules(): Promise<void> {
    await this.page.goto(ReportsPage.schedulesRoute);
  }

  get firstPdfButton(): Locator {
    return this.page.getByRole('button', { name: /pdf/i }).first();
  }
}
