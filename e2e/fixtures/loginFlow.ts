import type { Page } from '@playwright/test';
import { type UserCredentials } from './credentials';

export const loginAs = async (
  page: Page,
  user: UserCredentials,
  redirectPath: string = '/dashboard',
) => {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(user.email);
  await page.getByLabel(/password|şifre/i).fill(user.password);
  await page.getByRole('button', { name: /sign in|log in|giriş/i }).click();
  await page.waitForURL((url) => url.pathname.startsWith(redirectPath), { timeout: 20_000 });
};
