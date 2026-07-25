import { test, expect } from '@playwright/test';

const marketingPaths = [
  '/',
  '/features',
  '/how-it-works',
  '/pricing',
  '/security',
  '/privacy',
  '/ai-transparency',
  '/download',
  '/legal/privacy',
  '/login',
  '/register',
];

for (const path of marketingPaths) {
  test(`marketing page loads: ${path}`, async ({ page, baseURL }) => {
    await page.goto(`${baseURL}${path}`, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('article h1')).toBeVisible();
    await expect(page.getByText(/requires qualified legal review/i)).toBeVisible();
  });
}

test('marketing download page has no Razorpay checkout', async ({ page, baseURL }) => {
  await page.goto(`${baseURL}/download`);
  const body = await page.content();
  expect(body.toLowerCase()).not.toContain('razorpay');
  expect(body.toLowerCase()).not.toContain('checkout.razorpay.com');
});
