import { defineConfig, devices } from '@playwright/test';

const apiBaseUrl = process.env['SCOPESEAL_API_URL'] ?? 'http://localhost:5080';
const marketingBaseUrl = process.env['SCOPESEAL_MARKETING_URL'] ?? 'http://localhost:4201';

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
      use: { ...devices['Desktop Chrome'] },
      testMatch: /marketing-smoke\.spec\.ts/,
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
  },
});
