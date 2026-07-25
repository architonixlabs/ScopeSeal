import { defineConfig, devices } from '@playwright/test';

const apiBaseUrl = process.env['SCOPESEAL_API_URL'] ?? 'http://localhost:5080';
const marketingBaseUrl = process.env['SCOPESEAL_MARKETING_URL'] ?? 'http://localhost:4201';
const productBaseUrl = process.env['SCOPESEAL_PRODUCT_URL'] ?? 'http://localhost:4200';
const adminBaseUrl = process.env['SCOPESEAL_ADMIN_URL'] ?? 'http://localhost:4202';

const privateNetworkLaunchOptions = {
  args: [
    '--disable-features=BlockInsecurePrivateNetworkRequests,PrivateNetworkAccessSendPreflights,PrivateNetworkAccessRespectPreflightResults',
  ],
};

function usesPrivateNetwork(url: string) {
  return /\/\/192\.168\.|\/\/10\.|\/\/172\.(1[6-9]|2\d|3[01])\./.test(url);
}

const chromiumUse = {
  ...devices['Desktop Chrome'],
  ...(usesPrivateNetwork(productBaseUrl) ||
  usesPrivateNetwork(marketingBaseUrl) ||
  usesPrivateNetwork(adminBaseUrl)
    ? { launchOptions: privateNetworkLaunchOptions }
    : {}),
};

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  workers: 1,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    trace: 'on-first-retry',
    baseURL: marketingBaseUrl,
  },
  projects: [
    {
      name: 'marketing-chromium',
      use: { ...chromiumUse, baseURL: marketingBaseUrl },
      testMatch: /marketing-smoke\.spec\.ts/,
    },
    {
      name: 'product-chromium',
      use: { ...chromiumUse, baseURL: productBaseUrl },
      testMatch: /product-smoke\.spec\.ts/,
    },
    {
      name: 'admin-chromium',
      use: { ...chromiumUse, baseURL: adminBaseUrl },
      testMatch: /admin-smoke\.spec\.ts/,
    },
    {
      name: 'api-smoke',
      testMatch: /api-smoke\.spec\.ts/,
    },
  ],
  webServer: process.env['CI']
    ? undefined
    : [
        {
          command: 'npm run serve:ssr:marketing-site',
          url: marketingBaseUrl,
          reuseExistingServer: true,
          cwd: '.',
        },
      ],
  metadata: {
    apiBaseUrl,
    productBaseUrl,
    adminBaseUrl,
  },
});
