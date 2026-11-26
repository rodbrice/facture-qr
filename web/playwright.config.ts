import { defineConfig, devices } from '@playwright/test'

// The end-to-end test runs the front end as the API serves it (npm run build first) against a real
// PostgreSQL database with the migrations applied. ASPNETCORE_ENVIRONMENT=Development takes the
// synthetic creditor and the connection string of appsettings.Development.json; set
// ConnectionStrings__FactureQr to use another database.
const port = Number(process.env.FACTUREQR_E2E_PORT ?? 5080)
const baseURL = `http://127.0.0.1:${port}`

export default defineConfig({
  testDir: 'e2e',
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL,
    locale: 'fr-CH',
    timezoneId: 'Europe/Zurich',
    // The video of the run is the recording of the flow; CI keeps it as an artifact.
    video: 'on',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project ../src/FactureQr.Api --configuration Release --no-launch-profile',
    url: `${baseURL}/health`,
    timeout: 180_000,
    reuseExistingServer: !process.env.CI,
    stdout: 'ignore',
    stderr: 'pipe',
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: baseURL,
    },
  },
})
