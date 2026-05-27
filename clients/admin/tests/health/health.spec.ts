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

    // KPI strip labels.
    await expect(main.getByText("Liveness", { exact: true }).first()).toBeVisible();
    await expect(main.getByText("Readiness", { exact: true }).first()).toBeVisible();
    await expect(main.getByText("Checks healthy", { exact: true })).toBeVisible();
    await expect(main.getByText("Checks failing", { exact: true })).toBeVisible();

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
