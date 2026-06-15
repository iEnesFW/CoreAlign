import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '../BasePage';

export class InvoicesPage extends BasePage {
  static readonly route = '/dashboard/invoices';
  static readonly headingPattern = /invoices|faturalar/i;

  constructor(page: Page) {
    super(page, InvoicesPage.route);
  }

  get firstPdfButton(): Locator {
    return this.page.getByRole('button', { name: /pdf/i }).first();
  }
}
