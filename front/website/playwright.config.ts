import { defineConfig, devices } from "@playwright/test";

// Beside the backoffice's 4173, so both suites can run at once.
const port = 4174;

/** Runs against the static export in `out/`. The API is never called: specs intercept it with `page.route`. */
export default defineConfig({
  testDir: "e2e",
  forbidOnly: !!process.env.CI,
  reporter: process.env.CI ? "dot" : "list",
  use: {
    baseURL: `http://localhost:${port}`,
    locale: "es-ES",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: `serve out --listen ${port} --no-request-logging`,
    url: `http://localhost:${port}`,
    reuseExistingServer: !process.env.CI,
  },
});
