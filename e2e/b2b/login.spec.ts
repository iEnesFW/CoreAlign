import { expect, test } from '@playwright/test';
import { dealerUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('b2b dealer / login', () => {
  test('login form renders', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByLabel(/email/i)).toBeVisible();
  });

  test('dealer logs in and reaches customers page', async ({ page }) => {
    test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');
    await loginAs(page, dealerUser, '/');
    await page.goto('/customers');
    await expect(page.getByRole('heading', { name: /customers|müşteriler/i })).toBeVisible();
  });
});
