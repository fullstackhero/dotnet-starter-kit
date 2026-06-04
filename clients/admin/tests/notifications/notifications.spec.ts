import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

const NOTIF = {
  id: "n-1",
  type: "tenant.created",
  title: "New tenant provisioned",
  body: "Acme Corp finished provisioning.",
  link: null,
  source: "Multitenancy",
  metadataJson: "{}",
  readAtUtc: null,
  createdAtUtc: "2026-05-22T08:00:00Z",
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("notifications inbox", () => {
  test("shows the inbox-zero empty state (shell stubs notifications as [])", async ({ page }) => {
    // The shell already stubs **/api/v1/notifications** as []. Default filter
    // is "unread" → the "Nothing unread." copy.
    await page.goto("/notifications");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Notifications", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(main.getByText("Nothing unread.", { exact: true })).toBeVisible();
  });

  test("renders a notification row when the inbox is populated", async ({ page }) => {
    // Override the shell stub AFTER installAdminShellMocks so this wins. The
    // client builds the URL as `/api/v1/notifications/?unreadOnly=...` (note
    // the trailing slash before the query), so match that shape.
    await mockJsonResponse(page, "**/api/v1/notifications/?*", [NOTIF]);

    await page.goto("/notifications");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Notifications", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Row content from our mock.
    await expect(main.getByText("New tenant provisioned", { exact: true })).toBeVisible();
    await expect(main.getByText("Acme Corp finished provisioning.", { exact: true })).toBeVisible();
    // Unread → Mark read button.
    await expect(main.getByRole("button", { name: /mark read/i }).first()).toBeVisible();
  });

  test("has the unread/all filter and a Mark all read action", async ({ page }) => {
    await page.goto("/notifications");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Notifications", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(main.getByRole("button", { name: /mark all read/i })).toBeVisible();
    // The filter is a dropdown trigger (Radix button-based select); its default
    // value is "unread", so assert the trigger shows the "Unread" label.
    await expect(
      main.getByRole("button", { name: "Unread", exact: true }),
    ).toBeVisible();
  });
});
