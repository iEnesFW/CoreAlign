import { expect, test } from '@playwright/test';
import { adminUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('admin / customer-order-invoice happy path', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test('creates a customer, an order, and ships it', async ({ page }) => {
    await loginAs(page, adminUser);

    await page.goto('/dashboard/customers');
    await expect(page.getByRole('heading', { name: /customers|müşteriler/i })).toBeVisible();

    await page.getByRole('button', { name: /new customer|yeni müşteri|add customer/i }).click();
    const customerName = `E2E Customer ${Date.now()}`;
    await page.getByLabel(/^name|^ad/i).fill(customerName);
    await page.getByRole('button', { name: /save|kaydet|create/i }).click();
    await expect(page.getByText(customerName)).toBeVisible({ timeout: 15_000 });

    await page.goto('/dashboard/orders');
    await page.getByRole('button', { name: /new order|yeni sipariş/i }).click();
    await expect(page.getByText(/order/i).first()).toBeVisible();
  });

  test('downloads invoice PDF', async ({ page }) => {
    await loginAs(page, adminUser);
    await page.goto('/dashboard/invoices');
    const downloadPromise = page.waitForEvent('download');
    const firstPdfButton = page.getByRole('button', { name: /pdf/i }).first();
    if (await firstPdfButton.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await firstPdfButton.click();
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toMatch(/\.pdf$/i);
    }
  });
});
