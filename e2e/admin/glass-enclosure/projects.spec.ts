import { expect, test } from '@playwright/test';
import { skipIfNoStack } from '../../fixtures/credentials';
import { GlassProjectsPage } from '../../pages';

test.describe('admin / glass-enclosure / projects', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test('project list page loads with table and new-project action', async ({ page }) => {
    const projects = new GlassProjectsPage(page);
    await projects.goto();
    await expect(projects.projectsTable).toBeVisible();
    await expect(projects.newProjectButton).toBeVisible();
  });

  test('opens the new-project wizard from the list', async ({ page }) => {
    const projects = new GlassProjectsPage(page);
    await projects.goto();
    await projects.openNewProjectWizard();
    await expect(page).toHaveURL(/glass-enclosure\/projects\/new/);
  });

  test('opens an existing project into the designer when one exists', async ({ page }) => {
    const projects = new GlassProjectsPage(page);
    await projects.goto();
    const hasProject = await projects
      .firstProjectLink()
      .isVisible({ timeout: 5_000 })
      .catch(() => false);
    if (!hasProject) {
      test.skip(true, 'No existing glass projects in this fixture.');
      return;
    }
    await projects.openFirstProject();
    await expect(page).toHaveURL(/glass-enclosure\/projects\/[^/]+$/);
  });
});
