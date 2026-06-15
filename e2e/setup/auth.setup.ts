import { test as setup, type Page } from '@playwright/test';
import { authenticate } from '../fixtures/authForms';
import { baseUrlForRole } from '../fixtures/baseUrls';
import { roleProfiles, type RoleName } from '../fixtures/roles';
import { skipIfNoStack } from '../fixtures/credentials';

const persistAuthenticatedState = async (page: Page, role: RoleName) => {
  const profile = roleProfiles[role];
  await page.goto(baseUrlForRole[role]);
  await authenticate(page, profile.credentials, profile.postLoginUrlPattern);
  await page.context().storageState({ path: profile.storageStatePath });
};

setup('authenticate admin', async ({ page }) => {
  setup.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');
  await persistAuthenticatedState(page, 'admin');
});

setup('authenticate customer-portal', async ({ page }) => {
  setup.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');
  await persistAuthenticatedState(page, 'customer-portal');
});

setup('authenticate b2b', async ({ page }) => {
  setup.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');
  await persistAuthenticatedState(page, 'b2b');
});
