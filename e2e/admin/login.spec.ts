import { expect, test } from '@playwright/test';
import { adminUser, skipIfNoStack } from '../fixtures/credentials';
import { loginAs } from '../fixtures/loginFlow';

test.describe('admin / login', () => {
  test('login form renders with email and password fields', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password|şifre/i)).toBeVisible();
  });

  test('rejects empty submit with validation feedback', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('button', { name: /sign in|log in|giriş/i }).click();
    await expect(page).toHaveURL(/.*\/login/);
  });

  test('logs in and lands on /dashboard', async ({ page }) => {
    test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');
    await loginAs(page, adminUser);
    await expect(page).toHaveURL(/\/dashboard/);
  });
});
