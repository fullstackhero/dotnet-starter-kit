import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

// Shared role list used by the directory's role filter dropdown.
const ROLES = [
  { id: "role-admin", name: "Admin", description: "Full access" },
  { id: "role-manager", name: "Manager", description: "Manages a team" },
];

const USERS = [
  {
    id: "u-bob",
    userName: "bob.patel",
    firstName: "Bob",
    lastName: "Patel",
    email: "bob.patel@root.com",
    isActive: true,
    emailConfirmed: true,
    phoneNumber: "+1 555 0100",
  },
  {
    id: "u-dana",
    userName: "dana.lee",
    firstName: "Dana",
    lastName: "Lee",
    email: "dana.lee@root.com",
    isActive: false,
    emailConfirmed: false,
  },
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  // Role filter dropdown source — page-specific, registered after shell.
  // The roles endpoint is paged (`PagedResponse<RoleDto>`); listRoles unwraps `.items`.
  await mockJsonResponse(page, "**/api/v1/identity/roles", paged(ROLES));
});

test.describe("users directory list", () => {
  test("renders the Directory heading and a user row from the mock data", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/search**",
      paged(USERS, { pageSize: 12, totalCount: 2 }),
    );

    await page.goto("/users");

    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "Directory", exact: true })).toBeVisible({
      timeout: 10_000,
    });
    // Account count line.
    await expect(main.getByText(/2 accounts on this tenant\./i)).toBeVisible();
    // A row from the mock. The name renders in both the mobile card and the
    // desktop row (responsive duplication) — scope to the desktop row's
    // username chip (`@bob.patel`, desktop-only) and the row buttons' names.
    await expect(main.getByText("@bob.patel", { exact: true })).toBeVisible();
    await expect(
      main.getByRole("button", { name: /^Bob Patel/ }).first(),
    ).toBeVisible();
    await expect(
      main.getByRole("button", { name: /^Dana Lee/ }).first(),
    ).toBeVisible();
  });

  test("shows the empty state when the search returns no users", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/search**",
      paged([], { pageSize: 12, totalCount: 0 }),
    );

    await page.goto("/users");

    const main = page.getByRole("main");
    await expect(main.getByText("No matches.", { exact: true })).toBeVisible({ timeout: 10_000 });
    // No search term / filters are active here, so the empty state shows the
    // "seed this tenant" prompt (the "adjust filters" copy only renders when a
    // search/filter is active).
    await expect(
      main.getByText("Register the first member to seed this tenant."),
    ).toBeVisible();
  });

  test("typing in the search box issues a search request with the term", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/search**",
      paged(USERS, { pageSize: 12, totalCount: 2 }),
    );

    await page.goto("/users");
    const search = page.getByPlaceholder("Search name, username, email…");
    await expect(search).toBeVisible({ timeout: 10_000 });

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/users/search") &&
        /[?&]Search=patel/i.test(r.url()),
      { timeout: 5_000 },
    );
    await search.fill("patel");
    const req = await reqPromise;
    expect(req.url()).toMatch(/Search=patel/i);
  });
});

test.describe("users create form", () => {
  test("renders the identity + credential fields and the create action", async ({ page }) => {
    // /users/new now redirects to /users — creation is a modal dialog opened
    // from the list's "New user" trigger.
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/search**",
      paged(USERS, { pageSize: 12, totalCount: 2 }),
    );
    await page.goto("/users");
    await page.getByRole("button", { name: "New user", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "New account", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(dialog.getByLabel(/^First name/)).toBeVisible();
    await expect(dialog.getByLabel(/^Last name/)).toBeVisible();
    await expect(dialog.getByLabel(/^Username/)).toBeVisible();
    await expect(dialog.getByLabel(/^Email/)).toBeVisible();
    await expect(dialog.getByLabel(/^Password/, { exact: false })).toBeVisible();
    await expect(dialog.getByLabel(/^Confirm password/)).toBeVisible();

    await expect(
      dialog.getByRole("button", { name: "Create account", exact: true }),
    ).toBeVisible();
  });

  test("filling the form and submitting POSTs to /users/register", async ({ page }) => {
    await page.route("**/api/v1/identity/users/register", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: "u-new", message: "Confirmation email queued." }),
      });
    });
    // Success navigates to the new user's detail page — mock its loads.
    await mockJsonResponse(page, "**/api/v1/identity/users/u-new", {
      ...USERS[0],
      id: "u-new",
      userName: "m.chen",
    });
    await mockJsonResponse(page, "**/api/v1/identity/users/u-new/roles", []);
    await mockJsonResponse(page, "**/api/v1/identity/users/u-new/sessions", []);
    // Need the list to render so the "New user" dialog trigger is present.
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/search**",
      paged(USERS, { pageSize: 12, totalCount: 2 }),
    );

    await page.goto("/users");
    await page.getByRole("button", { name: "New user", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^First name/)).toBeVisible({ timeout: 10_000 });

    await dialog.getByLabel(/^First name/).fill("Mei");
    await dialog.getByLabel(/^Last name/).fill("Chen");
    await dialog.getByLabel(/^Username/).fill("m.chen");
    await dialog.getByLabel(/^Email/).fill("m.chen@root.com");
    await dialog.getByLabel(/^Password/, { exact: false }).first().fill("Sup3rSecret!");
    await dialog.getByLabel(/^Confirm password/).fill("Sup3rSecret!");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/identity/users/register") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Create account", exact: true }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body).toMatchObject({
      firstName: "Mei",
      lastName: "Chen",
      userName: "m.chen",
      email: "m.chen@root.com",
      password: "Sup3rSecret!",
      confirmPassword: "Sup3rSecret!",
    });
  });

  test("client-side validation blocks submit on an invalid username", async ({ page }) => {
    let posted = false;
    await page.route("**/api/v1/identity/users/register", async (route) => {
      if (route.request().method() === "POST") posted = true;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: "x" }),
      });
    });

    // Need the list to render so the "New user" dialog trigger is present.
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/search**",
      paged(USERS, { pageSize: 12, totalCount: 2 }),
    );

    await page.goto("/users");
    await page.getByRole("button", { name: "New user", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^First name/)).toBeVisible({ timeout: 10_000 });

    // Leave email blank so native email validation doesn't pre-empt zod.
    // Username "1x" violates the start-with-a-letter / length rule.
    await dialog.getByLabel(/^First name/).fill("Mei");
    await dialog.getByLabel(/^Last name/).fill("Chen");
    await dialog.getByLabel(/^Username/).fill("1x");
    await dialog.getByLabel(/^Password/, { exact: false }).first().fill("Sup3rSecret!");
    await dialog.getByLabel(/^Confirm password/).fill("Sup3rSecret!");

    await dialog.getByRole("button", { name: "Create account", exact: true }).click();

    await expect(dialog.getByText(/Start with a letter\./i)).toBeVisible();
    expect(posted).toBe(false);
  });
});

test.describe("user detail page", () => {
  const USER = USERS[0];

  test("loads the user and renders identity, roles, and sessions sections", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER.id}`, USER);
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER.id}/roles`, [
      { roleId: "role-admin", roleName: "Admin", description: "Full access", enabled: true },
      { roleId: "role-manager", roleName: "Manager", description: "Manages a team", enabled: false },
    ]);
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER.id}/sessions`, [
      {
        id: "s-1",
        userId: USER.id,
        ipAddress: "10.0.0.1",
        deviceType: "desktop",
        browser: "Chrome",
        browserVersion: "120",
        operatingSystem: "Windows",
        createdAt: "2026-05-20T10:00:00Z",
        lastActivityAt: "2026-05-23T09:00:00Z",
        expiresAt: "2026-06-23T09:00:00Z",
        isActive: true,
        isCurrentSession: true,
      },
    ]);

    await page.goto(`/users/${USER.id}`);

    const main = page.getByRole("main");
    // Heading shows the full name (appears in PageHeader h1 + header h2 → use heading role + first).
    await expect(
      main.getByRole("heading", { name: "Bob Patel" }).first(),
    ).toBeVisible({ timeout: 10_000 });

    // Status + email badges.
    await expect(main.getByText("Active", { exact: true }).first()).toBeVisible();
    await expect(main.getByText(/Email confirmed/i).first()).toBeVisible();

    // Section headings. Identity + roles now render via SettingsSection (h2
    // with plain titles); the sessions card still uses FormSection ("\ Sessions").
    await expect(
      main.getByRole("heading", { name: "Identity card", exact: true }),
    ).toBeVisible();
    await expect(
      main.getByRole("heading", { name: "Role assignment", exact: true }),
    ).toBeVisible();
    await expect(main.getByText(/\\ Sessions/i).first()).toBeVisible();

    // Role chips rendered from the roles endpoint.
    await expect(main.getByText("Admin", { exact: true })).toBeVisible();
    await expect(main.getByText("Manager", { exact: true })).toBeVisible();

    // Session row rendered from the sessions endpoint.
    await expect(main.getByText(/Chrome 120 on Windows/i)).toBeVisible();
  });

  test("surfaces a server error band when the user fails to load", async ({ page }) => {
    await page.route(`**/api/v1/identity/users/${USER.id}`, (route) =>
      route.fulfill({
        status: 404,
        headers: { "Content-Type": "application/problem+json" },
        body: JSON.stringify({ title: "Not Found", status: 404, detail: "User not found." }),
      }),
    );
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER.id}/roles`, []);
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER.id}/sessions`, []);

    await page.goto(`/users/${USER.id}`);

    await expect(page.getByText(/User not found\./i)).toBeVisible({ timeout: 10_000 });
  });
});
