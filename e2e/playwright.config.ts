import { defineConfig, devices } from '@playwright/test';
import { adminBaseUrl, b2bBaseUrl, customerBaseUrl } from './fixtures/baseUrls';
import { roleProfiles } from './fixtures/roles';

const isCI = Boolean(process.env.CI);
const reuseExistingServer = !isCI;
const serverTimeout = 120_000;

export default defineConfig({
  testDir: '.',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  forbidOnly: isCI,
  retries: isCI ? 2 : 0,
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  use: {
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      command: 'npm run dev',
      url: adminBaseUrl,
      cwd: '..',
      reuseExistingServer,
      timeout: serverTimeout,
    },
    {
      command: 'npm run dev --prefix apps/customer-portal',
      url: customerBaseUrl,
      cwd: '..',
      reuseExistingServer,
      timeout: serverTimeout,
    },
    {
      command: 'npm run dev --prefix apps/b2b',
      url: b2bBaseUrl,
      cwd: '..',
      reuseExistingServer,
      timeout: serverTimeout,
    },
  ],
  projects: [
    {
      name: 'setup',
      testDir: './setup',
      testMatch: /.*\.setup\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'admin',
      testDir: './admin',
      dependencies: ['setup'],
      use: {
        ...devices['Desktop Chrome'],
        baseURL: adminBaseUrl,
        storageState: roleProfiles.admin.storageStatePath,
      },
    },
    {
      name: 'customer-portal',
      testDir: './customer-portal',
      dependencies: ['setup'],
      use: {
        ...devices['Desktop Chrome'],
        baseURL: customerBaseUrl,
        storageState: roleProfiles['customer-portal'].storageStatePath,
      },
    },
    {
      name: 'b2b',
      testDir: './b2b',
      dependencies: ['setup'],
      use: {
        ...devices['Desktop Chrome'],
        baseURL: b2bBaseUrl,
        storageState: roleProfiles.b2b.storageStatePath,
      },
    },
  ],
});
