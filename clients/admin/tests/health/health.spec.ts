import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// apiBase is "" in dev config, so the probes hit the origin: /health/live
// and /health/ready (anonymous, NO /api/v1 prefix).
const LIVE_HEALTHY = {
  status: "Healthy",
  results: [],
};

const READY_HEALTHY = {
  status: "Healthy",
  results: [
    {
      name: "npgsql",
      status: "Healthy",
      description: "PostgreSQL reachable",
      durationMs: 4.2,
      details: { host: "localhost" },
    },
    {
      name: "redis",
      status: "Healthy",
      description: "Cache reachable",
      durationMs: 1.1,
      details: null,
    },
  ],
};

const READY_UNHEALTHY = {
  status: "Unhealthy",
  results: [
    {
      name: "npgsql",
      status: "Unhealthy",
      description: "Connection refused",
      durationMs: 8000,
      details: null,
    },
  ],
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  // The Vite dev server proxies /health -> API (so probes reach the backend in
  // real dev), which shadows the SPA's /health route on a hard navigation.
  // Serve the SPA shell for the document request so client-side routing renders
  // the page; the probe fetches below are still intercepted by their own mocks.
  await page.route("http://localhost:5173/health", async (route) => {
    const res = await page.request.get("http://localhost:5173/");
    await route.fulfill({ status: 200, contentType: "text/html", body: await res.text() });
  });
});

test.describe("health probes", () => {
  test("renders the heading, KPI strip, and a healthy check name", async ({ page }) => {
    await mockJsonResponse(page, "**/health/live", LIVE_HEALTHY);
    await mockJsonResponse(page, "**/health/ready", READY_HEALTHY);

    await page.goto("/health");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Health", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // KPI strip labels render as the Stat component's mono-caps ".meta" crumb.
    // "Liveness"/"Readiness" also appear as ProbeSection (SettingsSection) <h2>
    // titles, so target the label element by its class rather than a bare text
    // match.
    const kpiLabel = (text: string) => main.locator("div.meta", { hasText: text });
    await expect(kpiLabel("Liveness")).toBeVisible();
    await expect(kpiLabel("Readiness")).toBeVisible();
    await expect(kpiLabel("Checks healthy")).toBeVisible();
    await expect(kpiLabel("Checks failing")).toBeVisible();

    // A check name from the readiness mock.
    await expect(main.getByText("npgsql", { exact: true })).toBeVisible();
    await expect(main.getByText("redis", { exact: true })).toBeVisible();
  });

  test("surfaces an unhealthy readiness probe", async ({ page }) => {
    await mockJsonResponse(page, "**/health/live", LIVE_HEALTHY);
    // 503 with a body — the client reads the body on 503.
    await mockJsonResponse(page, "**/health/ready", READY_UNHEALTHY, { status: 503 });

    await page.goto("/health");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Health", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // The Readiness probe section shows an Unhealthy badge + the failing check.
    await expect(main.getByText("Unhealthy", { exact: true }).first()).toBeVisible();
    await expect(main.getByText("npgsql", { exact: true })).toBeVisible();
    await expect(main.getByText("Connection refused", { exact: true })).toBeVisible();
  });
});
