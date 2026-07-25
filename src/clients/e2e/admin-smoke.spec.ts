import { test, expect } from '@playwright/test';

const adminBaseUrl = process.env['SCOPESEAL_ADMIN_URL'] ?? 'http://localhost:4202';

test.describe('Admin portal shell', () => {
  test.use({ baseURL: adminBaseUrl });

  test.skip(!process.env['SCOPESEAL_ADMIN_URL'] && !process.env['CI'], 'Requires admin URL (set SCOPESEAL_ADMIN_URL)');

  test('login page loads', async ({ page }) => {
    await page.goto('/login', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('h1')).toContainText('Operator sign-in');
  });

  test('unauthenticated root redirects to login', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/login/);
  });
});
