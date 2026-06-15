import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '../BasePage';

export class CustomersPage extends BasePage {
  static readonly route = '/dashboard/customers';
  static readonly headingPattern = /customers|müşteriler/i;

  constructor(page: Page) {
    super(page, CustomersPage.route);
  }

  get newCustomerButton(): Locator {
    return this.page.getByRole('button', {
      name: /new customer|yeni müşteri|add customer/i,
    });
  }

  get nameInput(): Locator {
    return this.page.getByLabel(/^name|^ad/i);
  }

  get saveButton(): Locator {
    return this.page.getByRole('button', { name: /save|kaydet|create|oluştur/i });
  }

  rowByText(text: string): Locator {
    return this.page.getByText(text);
  }
}
