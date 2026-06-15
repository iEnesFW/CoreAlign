import { expect, type Page } from '@playwright/test';
import { type UserCredentials } from './credentials';

const emailField = (page: Page) =>
  page.locator('input[type="email"], input[name="email"], input[autocomplete="email"]').first();

const passwordField = (page: Page) =>
  page
    .locator(
      'input[type="password"], input[name="password"], input[autocomplete="current-password"]',
    )
    .first();

const submitButton = (page: Page) => page.locator('form button[type="submit"]').first();

export const fillCredentialsAndSubmit = async (page: Page, user: UserCredentials) => {
  await emailField(page).fill(user.email);
  await passwordField(page).fill(user.password);
  await submitButton(page).click();
};

export const authenticate = async (page: Page, user: UserCredentials, expectedUrl: RegExp) => {
  await page.goto('/login');
  await expect(emailField(page)).toBeVisible();
  await fillCredentialsAndSubmit(page, user);
  await page.waitForURL(expectedUrl, { timeout: 20_000 });
};
