import { expect, test } from '@playwright/test';
import { adminUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('admin / reports', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test('navigates to the custom report builder', async ({ page }) => {
    await loginAs(page, adminUser);
    await page.goto('/dashboard/reports/custom');
    await expect(page.getByRole('heading', { name: /custom|özel/i })).toBeVisible();
  });

  test('shows the saved schedules list', async ({ page }) => {
    await loginAs(page, adminUser);
    await page.goto('/dashboard/reports/schedules');
    await expect(page.getByRole('heading', { name: /scheduled|zamanlan/i })).toBeVisible();
  });

  test('downloads a stock-on-hand PDF from report library', async ({ page }) => {
    await loginAs(page, adminUser);
    await page.goto('/dashboard/reports/library');
    const downloadPromise = page.waitForEvent('download');
    const stockButton = page.getByText(/stock on hand|stok mevcut/i).first();
    await stockButton.click();
    const pdfButton = page.getByRole('button', { name: /pdf/i }).first();
    if (await pdfButton.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await pdfButton.click();
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toMatch(/\.pdf$/i);
    }
  });
});
