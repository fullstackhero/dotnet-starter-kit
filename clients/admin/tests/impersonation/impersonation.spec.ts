import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

const ACTIVE_GRANT = {
  id: "g-active-1",
  jti: "jti-active-1",
  actorUserId: "u-actor",
  actorUserName: "rootadmin",
  actorTenantId: "root",
  impersonatedUserId: "u-target",
  impersonatedUserName: "alice@acme.com",
  impersonatedTenantId: "acme",
  reason: "Investigating a support ticket",
  startedAtUtc: "2026-05-23T10:00:00Z",
  expiresAtUtc: "2026-05-23T11:00:00Z",
  status: "Active",
};

const REVOKED_GRANT = {
  ...ACTIVE_GRANT,
  id: "g-revoked-1",
  jti: "jti-revoked-1",
  impersonatedUserName: "bob@acme.com",
  status: "Revoked",
  revokedAtUtc: "2026-05-23T10:30:00Z",
  revokedByUserName: "rootadmin",
  revokeReason: "Done",
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("impersonation grants list", () => {
  test("renders the heading, KPI strip, and an active grant row", async ({ page }) => {
    // Filtered list (default Status=Active) and the unfiltered KPI "all" query
    // share the same endpoint glob — one mock returning both grants serves both.
    await mockJsonResponse(
      page,
      "**/api/v1/identity/impersonation/grants*",
      [ACTIVE_GRANT, REVOKED_GRANT],
    );

    await page.goto("/impersonation");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Impersonation", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // KPI strip — assert via the unique per-Stat hint copy so we don't collide
    // with the status filter option / row badge that also read "Active" etc.
    await expect(main.getByText("in-flight tokens", { exact: true })).toBeVisible();
    await expect(main.getByText("operator clicked End", { exact: true })).toBeVisible();
    await expect(main.getByText("forcibly invalidated", { exact: true })).toBeVisible();
    await expect(main.getByText("reached natural TTL", { exact: true })).toBeVisible();

    // Active grant row — actor → impersonated. Default filter is Active, so
    // only the active grant appears in the list.
    await expect(main.getByText("alice@acme.com", { exact: true })).toBeVisible();
  });

  test("shows the empty state when there are no active grants", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/impersonation/grants*", []);

    await page.goto("/impersonation");

    const main = page.getByRole("main");
    await expect(
      main.getByText("No active impersonations.", { exact: true }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("the Revoke action is offered on an active grant", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/impersonation/grants*",
      [ACTIVE_GRANT],
    );

    await page.goto("/impersonation");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Impersonation", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // canRevoke needs Impersonation.Revoke (granted) + status Active.
    await expect(main.getByRole("button", { name: /revoke/i }).first()).toBeVisible();
  });
});
