import { defineConfig, devices } from '@playwright/test';
import { fileURLToPath } from 'node:url';

const here = fileURLToPath(new URL('.', import.meta.url));
const repoRoot = fileURLToPath(new URL('..', import.meta.url));

// The E2E database is always learnhub_e2e. The live `learnhub` database is never used here.
const dbHost = process.env.LEARNHUB_DB_HOST ?? '127.0.0.1';
const dbPort = process.env.LEARNHUB_DB_PORT ?? '3306';
const dbUser = process.env.LEARNHUB_DB_USER ?? 'test';
const dbPassword = process.env.LEARNHUB_DB_PASSWORD ?? 'test';
const connectionString =
  `Server=${dbHost};Port=${dbPort};User ID=${dbUser};Password=${dbPassword};Database=learnhub_e2e`;

export default defineConfig({
  testDir: `${here}tests`,
  globalSetup: `${here}global-setup.ts`,
  fullyParallel: false,
  workers: 1, // all tests share one database
  forbidOnly: !!process.env.CI,
  retries: 0,
  timeout: 60_000,
  reporter: [['list'], ['html', { open: 'never', outputFolder: `${here}playwright-report` }]],
  outputDir: `${here}test-results`,
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      // Started from the backend folder; dotnet run takes a while on a cold start.
      command: 'dotnet run --urls http://localhost:5080',
      cwd: `${repoRoot}backend/LearnHub.Api`,
      url: 'http://localhost:5080/api/courses',
      timeout: 180_000,
      reuseExistingServer: false, // never reuse a server that might be pointed at the live DB
      env: {
        ...process.env,
        LEARNHUB_DB: connectionString,
        LEARNHUB_NOW: '2026-10-01T12:00:00Z',
        DOTNET_ROLL_FORWARD: 'Major',
      } as Record<string, string>,
    },
    {
      command: 'npm run dev -- --port 5173 --strictPort',
      cwd: `${repoRoot}frontend`,
      url: 'http://localhost:5173',
      timeout: 180_000,
      reuseExistingServer: false,
    },
  ],
});
