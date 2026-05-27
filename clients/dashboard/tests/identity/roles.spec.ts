import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// listRoles returns a plain array (or { items }). Use a plain array.
const ROLES = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Admin",
    description: "Full access to everything",
    permissions: ["Permissions.Users.View", "Permissions.Users.Create"],
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    name: "Editor",
    description: "Can edit catalog content",
    permissions: ["Permissions.Products.Update"],
  },
];

const CATALOG = [
  {
    name: "Permissions.Users.View",
    description: "View users",
    resource: "Users",
    action: "View",
    isBasic: true,
    isRoot: false,
  },
  {
    name: "Permissions.Users.Create",
    description: "Create users",
    resource: "Users",
    action: "Create",
    isBasic: false,
    isRoot: false,
  },
  {
    name: "Permissions.Products.Update",
    description: "Update products",
    resource: "Products",
    action: "Update",
    isBasic: false,
    isRoot: false,
  },
];

test.describe("identity/roles — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/roles", ROLES);
  });

  test("renders the heading and a role row from the mock data", async ({ page }) => {
    await page.goto("/identity/roles");

    await expect(
      page.getByRole("heading", { name: "Roles", level: 1 }),
    ).toBeVisible();

    // Editor is a non-system role unique to our mock. It appears in both a
    // hidden mobile card and the desktop row — the desktop row is last in DOM
    // and visible at the default (lg) viewport.
    await expect(page.getByText("Editor", { exact: true }).last()).toBeVisible();
    await expect(page.getByText("Can edit catalog content").last()).toBeVisible();
  });

  test("client-side search filters the list to the empty state", async ({ page }) => {
    await page.goto("/identity/roles");
    await expect(page.getByText("Editor", { exact: true }).last()).toBeVisible();

    await page.getByPlaceholder("Search by name or description…").fill("zzzznope");

    await expect(
      page.getByRole("heading", { name: "No roles found", level: 2 }),
    ).toBeVisible();
    await expect(page.getByText(/nothing matches "zzzznope"/i)).toBeVisible();
  });

  test("opens the Create role dialog", async ({ page }) => {
    await page.goto("/identity/roles");
    await page.getByRole("button", { name: /new role/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(
      dialog.getByRole("heading", { name: /create a role/i }),
    ).toBeVisible();
    await expect(dialog.getByLabel("Name")).toBeVisible();
  });
});

test.describe("identity/roles/:roleId — detail", () => {
  const ROLE = ROLES[1]; // Editor — non-system so the editor is interactive
  const ROLE_ID = ROLE.id;

  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions/catalog", CATALOG);
    // getRoleWithPermissions hits /identity/{roleId}/permissions (no /roles/).
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE_ID}/permissions`, ROLE);
  });

  test("loads the role and shows the back link + details form", async ({ page }) => {
    await page.goto(`/identity/roles/${ROLE_ID}`);

    await expect(
      page.getByRole("heading", { name: "Editor", level: 1 }),
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /back to roles/i }),
    ).toBeVisible();

    // Name field is hydrated from the loaded role.
    await expect(page.getByLabel("Name")).toHaveValue("Editor");
  });

  test("renders the permissions editor with catalog groups", async ({ page }) => {
    await page.goto(`/identity/roles/${ROLE_ID}`);

    await expect(
      page.getByRole("heading", { name: "Permissions" }),
    ).toBeVisible();
    // groupPermissions buckets the catalog by resource → Users + Products.
    // Each group renders a tri-state "Toggle all {resource}" button — assert
    // on those to avoid colliding with the sidebar "Users" nav link.
    await expect(
      page.getByRole("button", { name: "Toggle all Users", exact: true }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Toggle all Products", exact: true }),
    ).toBeVisible();
  });
});
