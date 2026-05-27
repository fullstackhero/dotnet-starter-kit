import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// A pair of users for the search results. We assert on "Bob Patel" which
// only appears in our mock data — never in the shell defaults.
const USERS = [
  {
    id: "00000000-0000-0000-0000-0000000000b0",
    userName: "bob",
    firstName: "Bob",
    lastName: "Patel",
    email: "bob@acme.com",
    isActive: true,
    emailConfirmed: true,
  },
  {
    id: "00000000-0000-0000-0000-0000000000c0",
    userName: "carol",
    firstName: "Carol",
    lastName: "Smith",
    email: "carol@acme.com",
    isActive: false,
    emailConfirmed: false,
  },
];

const ROLES = [
  { id: "11111111-1111-1111-1111-111111111111", name: "Admin", description: "Full access" },
  { id: "22222222-2222-2222-2222-222222222222", name: "Manager", description: "Manage users" },
];

test.describe("identity/users — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    // The role filter combobox loads the role list on mount.
    await mockJsonResponse(page, "**/api/v1/identity/roles", ROLES);
  });

  test("renders the heading and a user row from the mock data", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged(USERS));

    await page.goto("/identity/users");

    await expect(
      page.getByRole("heading", { name: "Users", level: 1 }),
    ).toBeVisible();

    // Both a hidden mobile card AND a desktop row carry "Bob Patel". The
    // mobile card is rendered first in the DOM, the desktop row last — so the
    // last occurrence is the visible (md+) one at the default viewport.
    await expect(page.getByText("Bob Patel").last()).toBeVisible();
    await expect(page.getByText("bob@acme.com").last()).toBeVisible();
  });

  test("shows the empty state when the search returns no users", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([]));

    await page.goto("/identity/users");

    // No search/filter active → "No users yet" empty headline.
    await expect(
      page.getByRole("heading", { name: "No users yet", level: 2 }),
    ).toBeVisible();
    await expect(
      page.getByText(/register the first member to seed this tenant/i),
    ).toBeVisible();
  });

  test("typing in search re-queries and renders the matching user", async ({ page }) => {
    // Default: full list. After a "bob" search, return only Bob.
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged(USERS));

    await page.goto("/identity/users");
    await expect(page.getByText("Carol Smith").last()).toBeVisible();

    // Now narrow the mock to just Bob and type — the debounced query refires.
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([USERS[0]]));
    await page.getByPlaceholder("Search by name, username, or email…").fill("bob");

    await expect(page.getByText("Bob Patel").last()).toBeVisible();
    await expect(page.getByText("Carol Smith")).toHaveCount(0);
  });

  test("opens the Register member dialog", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged(USERS));

    await page.goto("/identity/users");
    await page.getByRole("button", { name: /register user/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(
      dialog.getByRole("heading", { name: /register a member/i }),
    ).toBeVisible();
    await expect(dialog.getByLabel("First name")).toBeVisible();
    await expect(dialog.getByLabel("Username")).toBeVisible();
  });
});

test.describe("identity/users/:userId — detail", () => {
  const USER = USERS[0];
  const USER_ID = USER.id;

  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER_ID}/roles`, [
      {
        roleId: ROLES[0].id,
        roleName: "Admin",
        description: "Full access",
        enabled: true,
      },
      {
        roleId: ROLES[1].id,
        roleName: "Manager",
        description: "Manage users",
        enabled: false,
      },
    ]);
    // Sessions are gated behind a permission TEST_USER lacks, but mock anyway.
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER_ID}/sessions`, []);
    // Most specific last: the by-id GET. (Roles/sessions globs are more
    // specific paths so they still win for their URLs.)
    await mockJsonResponse(page, `**/api/v1/identity/users/${USER_ID}`, USER);
  });

  test("loads the user and shows the back link + role assignment", async ({ page }) => {
    await page.goto(`/identity/users/${USER_ID}`);

    await expect(
      page.getByRole("heading", { name: "Bob Patel", level: 1 }),
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /back to users/i }),
    ).toBeVisible();

    // The roles section lists the staged role toggles from the mock.
    await expect(page.getByText("Admin", { exact: true })).toBeVisible();
    await expect(page.getByText("Manager", { exact: true })).toBeVisible();
  });

  test("renders the identity card fields from the mock", async ({ page }) => {
    await page.goto(`/identity/users/${USER_ID}`);

    await expect(
      page.getByRole("heading", { name: /identity card/i }),
    ).toBeVisible();
    // Email appears in the identity card profile row.
    await expect(page.getByText("bob@acme.com").first()).toBeVisible();
    // @username subtitle is shown in the hero.
    await expect(page.getByText("@bob").first()).toBeVisible();
  });
});
