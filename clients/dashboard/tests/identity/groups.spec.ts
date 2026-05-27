import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// listGroups returns a plain array (or { items }). Use a plain array.
const GROUPS = [
  {
    id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    name: "Engineering",
    description: "Engineers across web, mobile, and platform.",
    isDefault: false,
    isSystemGroup: false,
    memberCount: 3,
    roleIds: ["11111111-1111-1111-1111-111111111111"],
    roleNames: ["Admin"],
    createdAt: "2026-05-01T10:00:00Z",
  },
  {
    id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    name: "Everyone",
    description: null,
    isDefault: true,
    isSystemGroup: true,
    memberCount: 12,
    roleIds: [],
    roleNames: [],
    createdAt: "2026-04-01T10:00:00Z",
  },
];

const ROLES = [
  { id: "11111111-1111-1111-1111-111111111111", name: "Admin", description: "Full access" },
  { id: "22222222-2222-2222-2222-222222222222", name: "Manager", description: "Manage users" },
];

test.describe("identity/groups — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/groups**", GROUPS);
  });

  test("renders the heading and a group row from the mock data", async ({ page }) => {
    await page.goto("/identity/groups");

    await expect(
      page.getByRole("heading", { name: "Groups", level: 1 }),
    ).toBeVisible();

    // The name appears in both a hidden mobile card and the desktop row; the
    // desktop row is last in the DOM and visible at the default viewport.
    await expect(page.getByText("Engineering", { exact: true }).last()).toBeVisible();
    await expect(
      page.getByText("Engineers across web, mobile, and platform.").last(),
    ).toBeVisible();
  });

  test("shows the empty state when no groups exist", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/groups**", []);

    await page.goto("/identity/groups");

    await expect(
      page.getByRole("heading", { name: "No groups yet", level: 2 }),
    ).toBeVisible();
    await expect(
      page.getByText(/create the first group to bundle members and roles/i),
    ).toBeVisible();
  });

  test("opens the Create group dialog", async ({ page }) => {
    await page.goto("/identity/groups");
    await page.getByRole("button", { name: /new group/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(
      dialog.getByRole("heading", { name: /create a group/i }),
    ).toBeVisible();
    await expect(dialog.getByLabel("Name")).toBeVisible();
  });
});

test.describe("identity/groups/:groupId — detail", () => {
  const GROUP = GROUPS[0]; // Engineering — non-system, so it's editable
  const GROUP_ID = GROUP.id;

  const MEMBERS = [
    {
      userId: "00000000-0000-0000-0000-0000000000b0",
      userName: "bob",
      email: "bob@acme.com",
      firstName: "Bob",
      lastName: "Patel",
      addedAt: "2026-05-02T10:00:00Z",
      addedBy: null,
    },
  ];

  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/roles", ROLES);
    // user search only fires once the Add members dialog is open.
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([]));
    await mockJsonResponse(page, `**/api/v1/identity/groups/${GROUP_ID}/members`, MEMBERS);
    await mockJsonResponse(page, `**/api/v1/identity/groups/${GROUP_ID}`, GROUP);
  });

  test("loads the group and shows the back link + member", async ({ page }) => {
    await page.goto(`/identity/groups/${GROUP_ID}`);

    await expect(
      page.getByRole("heading", { name: "Engineering", level: 1 }),
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /back to groups/i }),
    ).toBeVisible();

    // The single member from the mock renders in the Members section.
    await expect(page.getByText("Bob Patel")).toBeVisible();
  });

  test("lists attached roles and opens the Add members dialog", async ({ page }) => {
    await page.goto(`/identity/groups/${GROUP_ID}`);

    // Roles list is fed by listRoles; both roles render as toggle rows.
    await expect(page.getByText("Admin", { exact: true })).toBeVisible();
    await expect(page.getByText("Manager", { exact: true })).toBeVisible();

    await page.getByRole("button", { name: /add members/i }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(
      dialog.getByRole("heading", { name: /pick members to add/i }),
    ).toBeVisible();
    await expect(
      dialog.getByPlaceholder("Search by name, username, or email…"),
    ).toBeVisible();
  });
});
