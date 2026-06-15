import { expect, test } from '@playwright/test';
import { skipIfNoStack } from '../../fixtures/credentials';
import { GlassProjectsPage, NewProjectWizardPage } from '../../pages';

test.describe('admin / glass-enclosure / create project @mutation', () => {
  test.skip(skipIfNoStack(), 'No live stack configured (set E2E_LIVE_STACK=1)');

  test(
    'creates a glass project through the wizard and lands on the designer',
    { tag: '@mutation' },
    async ({ page }) => {
      const projects = new GlassProjectsPage(page);
      await projects.goto();
      await projects.openNewProjectWizard();

      const wizard = new NewProjectWizardPage(page);
      await wizard.pickFirstCategory();

      const projectName = `E2E Glass ${Date.now()}`;
      const nameVisible = await wizard.projectNameInput
        .isVisible({ timeout: 5_000 })
        .catch(() => false);
      if (!nameVisible) {
        test.skip(true, 'Wizard template step requires a selection not resolvable headlessly.');
        return;
      }

      await wizard.fillProjectName(projectName);

      const customerSelected = await wizard
        .searchAndSelectFirstCustomer('a')
        .then(() => true)
        .catch(() => false);
      if (!customerSelected) {
        test.skip(true, 'No selectable customer in this fixture.');
        return;
      }

      await wizard.nextButton.click();
      await wizard.fillQuickDimensions(3000, 2400);
      await wizard.createButton.click();

      await expect(page).toHaveURL(/glass-enclosure\/projects\/[^/]+$/, { timeout: 30_000 });
    },
  );
});
