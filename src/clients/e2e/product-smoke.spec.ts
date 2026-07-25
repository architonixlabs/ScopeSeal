import { test, expect } from '@playwright/test';

const productBaseUrl = process.env['SCOPESEAL_PRODUCT_URL'] ?? 'http://localhost:4200';

test.describe('Product app shell', () => {
  test.use({ baseURL: productBaseUrl });

  test.skip(!process.env['SCOPESEAL_PRODUCT_URL'] && !process.env['CI'], 'Requires product URL (set SCOPESEAL_PRODUCT_URL)');

  test('product shell loads', async ({ page }) => {
    await page.goto('/', { waitUntil: 'networkidle' });
    await expect(page.locator('h1')).toContainText('ScopeSeal');
    await expect(page.locator('main')).toBeVisible();
  });

  test('health proxy responds via product host', async ({ request }) => {
    test.skip(process.env['CI'] === 'true', 'CI serves static product only; health proxy requires nginx edge');
    const response = await request.get(`${productBaseUrl}/health/ready`);
    expect(response.status()).toBe(200);
  });
});
