import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// The readiness probe lives at the ORIGIN (no /api/v1 prefix) and is
// fetched anonymously by src/api/health.ts → `${env.apiBase}/health/ready`.
// In the test config apiBase is "" (see public/config.json), so the call
// resolves same-origin to /health/ready.
const READY = "**/health/ready";

function readiness(
  status: "Healthy" | "Degraded" | "Unhealthy",
  results: Array<{
    name: string;
    status: "Healthy" | "Degraded" | "Unhealthy";
    description?: string | null;
    durationMs: number;
    details?: Record<string, unknown> | null;
  }>,
) {
  return { status, results };
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("system/health", () => {
  test("renders the 'All systems operational' headline + mocked checks when Healthy", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      READY,
      readiness("Healthy", [
        { name: "self", status: "Healthy", description: "Liveness", durationMs: 1.2 },
        { name: "postgres-db", status: "Healthy", description: "Primary database", durationMs: 12 },
        { name: "redis", status: "Healthy", durationMs: 3 },
      ]),
    );

    await page.goto("/system/health");

    // Hero status headline (Outfit h2) for the Healthy path.
    await expect(
      page.getByRole("heading", { name: /all systems operational/i }),
    ).toBeVisible();
    // Status pill word.
    await expect(page.getByText("Healthy", { exact: true }).first()).toBeVisible();

    // Dependencies section + one mocked check name.
    await expect(
      page.getByRole("heading", { name: "Dependencies", level: 2 }),
    ).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "postgres-db" }),
    ).toBeVisible();
    await expect(page.getByRole("heading", { name: "redis" })).toBeVisible();
  });

  test("shows the 'Disruption detected' headline when Unhealthy", async ({ page }) => {
    await mockJsonResponse(
      page,
      READY,
      readiness("Unhealthy", [
        { name: "self", status: "Healthy", durationMs: 1 },
        { name: "redis", status: "Unhealthy", description: "Connection refused", durationMs: 5000 },
      ]),
    );

    await page.goto("/system/health");

    await expect(
      page.getByRole("heading", { name: /disruption detected/i }),
    ).toBeVisible();
    await expect(page.getByRole("heading", { name: "redis" })).toBeVisible();
  });

  test("renders the empty-checks state when no checks are registered", async ({ page }) => {
    await mockJsonResponse(page, READY, readiness("Healthy", []));

    await page.goto("/system/health");

    // EmptyChecks renders its headline as a styled <p>, not a heading.
    await expect(page.getByText(/no checks registered/i)).toBeVisible();
    await expect(page.getByText(/none are reporting yet/i)).toBeVisible();
  });

  test("expanding a dependency row reveals its detail panel", async ({ page }) => {
    await mockJsonResponse(
      page,
      READY,
      readiness("Healthy", [
        {
          name: "hangfire",
          status: "Healthy",
          description: "Background jobs",
          durationMs: 8,
          details: { serverCount: "1", queues: "default" },
        },
      ]),
    );

    await page.goto("/system/health");

    const row = page.getByRole("button", { name: /hangfire/i });
    await expect(row).toBeVisible();
    await row.click();

    // Expanded panel exposes the latency-budget label + a detail key.
    await expect(page.getByText(/latency budget/i)).toBeVisible();
    await expect(page.getByText("serverCount")).toBeVisible();
  });
});
