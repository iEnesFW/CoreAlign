import { type Locator, type Page } from '@playwright/test';
import { BasePage } from '../BasePage';

export class OrdersPage extends BasePage {
  static readonly route = '/dashboard/orders';
  static readonly headingPattern = /orders|siparişler/i;

  constructor(page: Page) {
    super(page, OrdersPage.route);
  }

  get newOrderButton(): Locator {
    return this.page.getByRole('button', { name: /new order|yeni sipariş/i });
  }
}
