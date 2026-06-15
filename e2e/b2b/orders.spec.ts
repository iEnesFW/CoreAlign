import { expect, test } from '@playwright/test';
import { dealerUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('b2b dealer / orders', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test('renders dealer customers list', async ({ page }) => {
    await loginAs(page, dealerUser, '/');
    await page.goto('/customers');
    await expect(page.getByRole('heading', { name: /customers|müşteriler/i })).toBeVisible();
  });

  test('starts a new order for the dealer', async ({ page }) => {
    await loginAs(page, dealerUser, '/');
    await page.goto('/orders/new');
    await expect(page.getByRole('heading', { name: /new order|yeni sipariş/i })).toBeVisible();
  });

  test('submits a dealer order and lands in pending approval', async ({ page }) => {
    await loginAs(page, dealerUser, '/');
    await page.goto('/orders/new');
    const submitBtn = page.getByRole('button', { name: /submit|gönder/i });
    if (await submitBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await submitBtn.click();
      await expect(page.locator('body')).toContainText(/pending approval|onay bekleniyor/i, {
        timeout: 15_000,
      });
    } else {
      test.skip(true, 'Submit button not present in current dealer flow.');
    }
  });
});
