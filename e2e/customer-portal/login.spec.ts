import { expect, test } from '@playwright/test';
import { customerUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('customer-portal / login', () => {
  test('login form renders', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByLabel(/email/i)).toBeVisible();
  });

  test('logs in and lands on the dashboard or invoices view', async ({ page }) => {
    test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');
    await loginAs(page, customerUser, '/');
    await expect(page).toHaveURL(/\/(invoices|dashboard|portal)/);
  });
});
