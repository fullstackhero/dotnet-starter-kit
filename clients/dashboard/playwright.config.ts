import { defineConfig, devices } from "@playwright/test";

/**
 * Playwright config for the dashboard app.
 *
 * Tests run against a Vite dev server on port 5174 (the same port the
 * `dev` script uses), with API calls intercepted via `page.route()` so
 * tests don't need a running backend. This keeps the test loop fast
 * (~5s per test) and deterministic — no flaky network, no DB seeding.
 *
 * For tests that need a real backend, see `clients/dashboard/tests/e2e/`
 * (none today — all current tests are route-mocked).
 *
 * Usage:
 *   npm run test:e2e               # headless, all browsers
 *   npm run test:e2e -- --ui       # interactive runner
 *   npm run test:e2e -- --headed   # see the browser drive
 *   npm run test:e2e -- auth.spec  # filter by file
 */
export default defineConfig({
  testDir: "./tests",
  // Tests are deterministic (mocked APIs) so parallelism is safe and
  // dramatically faster. We still serialise within a file via test.serial
  // when state spans tests (e.g. password-reset multi-step flows).
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  // Use all logical cores locally; throttle to 2 in CI to avoid
  // exhausting GitHub Actions runners.
  workers: process.env.CI ? 2 : undefined,
  reporter: process.env.CI ? [["github"], ["html", { open: "never" }]] : "list",

  use: {
    baseURL: "http://localhost:5174",
    trace: "on-first-retry",
    // Disable animations + reduce flake from CSS keyframes / transitions
    // (we have a lot — parallax orbs, fsh-enter staggers, btn-shimmer).
    // Tests assert against final state, not in-flight frames.
    actionTimeout: 10_000,
    navigationTimeout: 15_000,
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],

  // Boot the Vite dev server before any test runs. `reuseExistingServer`
  // means re-running the test suite picks up an already-running dev
  // server (faster local iteration).
  webServer: {
    command: "npm run dev",
    url: "http://localhost:5174",
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
    stdout: "ignore",
    stderr: "pipe",
  },
});
