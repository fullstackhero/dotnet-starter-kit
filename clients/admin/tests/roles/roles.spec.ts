import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// listRoles returns a bare array (not paged). System roles Admin/Basic sort first.
const ROLES = [
  { id: "role-manager", name: "Manager", description: "Manages a team" },
  { id: "role-admin", name: "Admin", description: "Full system access" },
  { id: "role-basic", name: "Basic", description: "Default authenticated role" },
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("roles list", () => {
  test("renders the Roles heading and a role row from the mock data", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/roles", ROLES);

    await page.goto("/roles");

    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "Roles", exact: true })).toBeVisible({
      timeout: 10_000,
    });
    await expect(main.getByText("Manager", { exact: true })).toBeVisible();
    await expect(main.getByText("Admin", { exact: true })).toBeVisible();
    await expect(main.getByText("Basic", { exact: true })).toBeVisible();
    // System roles get a "system" badge.
    await expect(main.getByText("system", { exact: true }).first()).toBeVisible();
  });

  test("shows the empty state when no roles are defined", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/roles", []);

    await page.goto("/roles");

    const main = page.getByRole("main");
    await expect(main.getByText("No roles defined yet.", { exact: true })).toBeVisible({
      timeout: 10_000,
    });
    await expect(main.getByText("Create your first role to start bundling permissions.")).toBeVisible();
  });
});

test.describe("roles create form", () => {
  test("renders the name + description fields and the create action", async ({ page }) => {
    await page.goto("/roles/new");

    await expect(
      page.getByRole("heading", { name: "New role", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.getByLabel(/^Name/)).toBeVisible();
    await expect(page.getByLabel(/^Description/)).toBeVisible();

    await expect(
      page.getByRole("button", { name: "Create role", exact: true }),
    ).toBeVisible();
  });

  test("filling the form and submitting POSTs to /identity/roles with an empty id", async ({ page }) => {
    await page.route("**/api/v1/identity/roles", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: "role-support", name: "Support agent", description: "Inbound support" }),
      });
    });
    // Success navigates to the role detail page — mock its permissions load.
    await mockJsonResponse(page, "**/api/v1/identity/role-support/permissions", {
      id: "role-support",
      name: "Support agent",
      description: "Inbound support",
      permissions: [],
    });

    await page.goto("/roles/new");
    await expect(page.getByLabel(/^Name/)).toBeVisible({ timeout: 10_000 });

    await page.getByLabel(/^Name/).fill("Support agent");
    await page.getByLabel(/^Description/).fill("Inbound support");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/identity/roles") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: "Create role", exact: true }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body).toMatchObject({
      id: "",
      name: "Support agent",
      description: "Inbound support",
    });
  });

  test("client-side validation blocks submit when the name is too short", async ({ page }) => {
    let posted = false;
    await page.route("**/api/v1/identity/roles", async (route) => {
      if (route.request().method() === "POST") posted = true;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: "x", name: "x" }),
      });
    });

    await page.goto("/roles/new");
    await expect(page.getByLabel(/^Name/)).toBeVisible({ timeout: 10_000 });

    await page.getByLabel(/^Name/).fill("A");
    await page.getByRole("button", { name: "Create role", exact: true }).click();

    await expect(page.getByText(/At least 2 characters\./i)).toBeVisible();
    expect(posted).toBe(false);
  });
});

test.describe("role detail permission matrix", () => {
  const ROLE = {
    id: "role-manager",
    name: "Manager",
    description: "Manages a team",
    permissions: ["Permissions.Users.View", "Permissions.Roles.View"],
  };

  test("loads the role and renders the profile + permission catalog groups", async ({ page }) => {
    // NOTE: path is /identity/{id}/permissions — NO /roles/ segment.
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);

    const main = page.getByRole("main");
    // PageHeader title shows the role name.
    await expect(main.getByRole("heading", { name: "Manager", exact: true })).toBeVisible({
      timeout: 10_000,
    });

    // Hydrated profile name field.
    await expect(main.getByLabel(/^Name/)).toHaveValue("Manager");

    // Section heading + catalog groups (rendered client-side from PERMISSION_CATALOG).
    // FormSection renders the title as "\ Permissions".
    await expect(main.getByText(/\\ Permissions/i).first()).toBeVisible();
    await expect(main.getByRole("heading", { name: "Users", exact: true })).toBeVisible();
    await expect(main.getByRole("heading", { name: "Roles", exact: true })).toBeVisible();
    await expect(main.getByRole("heading", { name: "Sessions", exact: true })).toBeVisible();

    // A specific permission entry from the catalog.
    await expect(main.getByText("Permissions.Users.View", { exact: true })).toBeVisible();
  });

  test("toggling a permission and saving PUTs to /identity/{id}/permissions", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);
    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "Manager", exact: true })).toBeVisible({
      timeout: 10_000,
    });

    // Toggle a not-yet-granted permission ("Create users").
    await main.getByText("Create users", { exact: true }).click();

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().endsWith(`/api/v1/identity/${ROLE.id}/permissions`) && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await main.getByRole("button", { name: /save permissions/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.roleId).toBe(ROLE.id);
    expect(body.permissions).toContain("Permissions.Users.Create");
    // Pre-existing grants are preserved.
    expect(body.permissions).toContain("Permissions.Users.View");
  });
});
