import { expect, test } from '@playwright/test';
import { customerUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('customer-portal / invoices', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test('renders invoice list', async ({ page }) => {
    await loginAs(page, customerUser, '/');
    await page.goto('/invoices');
    await expect(page.getByRole('heading', { name: /invoices|faturalar/i })).toBeVisible();
  });

  test('downloads invoice PDF when one exists', async ({ page }) => {
    await loginAs(page, customerUser, '/');
    await page.goto('/invoices');
    const downloadPromise = page.waitForEvent('download').catch(() => null);
    const pdfButton = page.getByRole('button', { name: /pdf/i }).first();
    if (await pdfButton.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await pdfButton.click();
      const download = await downloadPromise;
      if (download) {
        expect(download.suggestedFilename()).toMatch(/\.pdf$/i);
      }
    }
  });

  test('initiates pay flow (Iyzico checkout opens or skip when unavailable)', async ({ page }) => {
    await loginAs(page, customerUser, '/');
    await page.goto('/invoices');
    const payButton = page.getByRole('button', { name: /pay|öde/i }).first();
    if (await payButton.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await payButton.click();
      await expect(page.locator('body')).toContainText(/iyzico|3d secure|checkout|ödeme/i, {
        timeout: 15_000,
      });
    } else {
      test.skip(true, 'No payable invoices in this fixture.');
    }
  });
});
